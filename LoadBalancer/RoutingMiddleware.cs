namespace LoadBalancer.API;

public class RoutingMiddleware
{
    private readonly RequestDelegate _next;

    public RoutingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRouter router, IConfiguration configuration)
    {
        var host = configuration["Settings:Backends:0:Host"];
        var port = configuration["Settings:Backends:0:Port"];
        var targetUrl = $"http://{host}:{port}";
        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Backend URL is not configured");
            return;
        }
        await router.RouteAsync(context, targetUrl);
    }
}