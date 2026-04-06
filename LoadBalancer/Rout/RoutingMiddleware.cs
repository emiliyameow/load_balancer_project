using IRouter = LoadBalancer.API.Rout.IRouter;
using LoadBalancer.API.ServiceCache;

namespace LoadBalancer.API.Rout;

public class RoutingMiddleware
{
    private readonly RequestDelegate _next;

    public RoutingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRouter router, ServiceCacheHandler serversCache)
    {
        // получаем список серверов из кэша
        var backendsConfig = serversCache.GetInstances("users-service")
            .Select(backendsConfig => new BackendConfig()
            {
                Host = backendsConfig.Host,
                Port = backendsConfig.Port,
                Name = backendsConfig.Id
            })
            .ToList();

        // берем первый сервер (для тестирования)
        var server_1 = backendsConfig?.FirstOrDefault();
        if (server_1 == null)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Backend is not found");
            return;
        }

        // формируем адрес сервера
        var targetUrl = $"{server_1.Address}";

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