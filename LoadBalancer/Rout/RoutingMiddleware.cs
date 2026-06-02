using LoadBalancer.API.ServiceCache;
using LoadBalancer.API.Balance;
using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.ServiceDiscovery;
using System.Collections.Immutable;

namespace LoadBalancer.API.Rout;

public class RoutingMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;
    private const string DefaultServiceName = "users-service";

    public async Task InvokeAsync(
        HttpContext context, 
        IRouter router, 
        ServiceCacheHandler serversCache,
        HealthCache healthCache,
        BalanceAlgorithm balanceAlgorithm,
        BackendLoadTracker loadTracker,
        IServiceRegistry serviceRegistry)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var allServerConditions = serversCache.GetInstances(DefaultServiceName).ToList();
        if (allServerConditions.Count == 0)
        {
            try
            {
                allServerConditions = await LoadServiceInstancesAsync(
                    serviceRegistry,
                    serversCache,
                    DefaultServiceName,
                    context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("Service registry is unavailable");
                return;
            }
        }

        // фильтруем по только health серверам и берем свежую нагрузку из health cache
        var serverConditions = new List<ServerCondition>();
        foreach (var server in allServerConditions)
        {
            if (!healthCache.TryGet(server.ServerInfo.Address, out var health) || !health.IsAlive)
                continue;

            serverConditions.Add(new ServerCondition
            {
                ServerInfo = server.ServerInfo,
                IsAlive = true,
                Weight = health.Weight,
                LatencyMs = health.LatencyMs,
                CheckedAt = health.CheckedAt,
                Error = health.Error
            });
        }

        BackendLoadTracker.BackendReservation reservation;
        try
        {
            reservation = loadTracker.Reserve(serverConditions, balanceAlgorithm);
        }
        catch (BalanceException ex)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync($"Backend selection failed: {ex.Message}");
            return;
        }
        catch (Exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Internal balancing error");
            return;
        }

        using (reservation)
        {
            var selectedServer = reservation.Server;
            var targetUrl = selectedServer.ServerInfo.Address;

            context.Response.Headers["X-Balancer-Backend-Name"] = selectedServer.ServerInfo.Name;
            context.Response.Headers["X-Balancer-Backend-Address"] = selectedServer.ServerInfo.Address;
            context.Response.Headers["X-Balancer-Backend-Weight"] = selectedServer.Weight.ToString();
            context.Response.Headers["X-Balancer-Backend-Active"] = reservation.ActiveRequests.ToString();
            context.Response.Headers["X-Balancer-Backend-Effective-Weight"] = reservation.EffectiveWeight.ToString();

            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("Backend URL is not configured");
                return;
            }

            await router.RouteAsync(context, targetUrl);
        }
    }

    private static async Task<List<ServerCondition>> LoadServiceInstancesAsync(
        IServiceRegistry serviceRegistry,
        ServiceCacheHandler serversCache,
        string serviceName,
        CancellationToken ct)
    {
        var services = await serviceRegistry.GetServicesAsync(ct);
        if (!services.TryGetValue(serviceName, out var instances))
            return [];

        serversCache.AddOrUpdateService(serviceName, instances.ToImmutableList());
        return instances;
    }
}
