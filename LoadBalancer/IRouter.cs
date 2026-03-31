using Microsoft.AspNetCore.Http;

namespace LoadBalancer.API;

public interface IRouter
{
    Task RouteAsync(HttpContext context, string? targetUrl);
}