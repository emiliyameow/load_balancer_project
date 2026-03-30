namespace LoadBalancer.API.Rout;

public class RoutingMiddleware
{
    private readonly RequestDelegate _next;

    public RoutingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRouter router, IConfiguration configuration)
    {
        var backendsConfig = configuration
        .GetSection("Settings:Backends")
        .Get<List<BackendConfig>>();

        var server_1 = backendsConfig?.FirstOrDefault();

        if (server_1 == null)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Backend is not found");
            return;
        }

        var targetUrl = $"http://{server_1.Address}";

        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Backend URL is not configured");
            return;
        }
        await router.RouteAsync(context, targetUrl);
    }
}