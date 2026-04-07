using IRouter = LoadBalancer.API.Rout.IRouter;
using LoadBalancer.API.ServiceCache;
using LoadBalancer.API.Balance;
using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Rout;

public class RoutingMiddleware
{
    private readonly RequestDelegate _next;

    public RoutingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        IRouter router, 
        ServiceCacheHandler serversCache,
        BalanceAlgoritm balanceAlgoritm)
    {
        // получаем список серверов из кэша
        var serverConditions = serversCache.GetInstances("users-service").ToList();

        // берем первый сервер (для тестирования)
        ServerCondition selectedServer;
        try
        {
            selectedServer = balanceAlgoritm.GetFreeServer(serverConditions);
        }
        catch (BalanceException ex)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Backend is not found");
            await context.Response.WriteAsync($"Backend selection failed: {ex.Message}");
            return;
        }
        catch (Exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Internal balancing error");
            return;
        }

        // формируем адрес сервера
        var targetUrl = $"{selectedServer.ServerInfo.Address}";

        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Backend URL is not configured");
            return;
        }

        // передаем контекст в сервер
        await router.RouteAsync(context, targetUrl);
    }
}