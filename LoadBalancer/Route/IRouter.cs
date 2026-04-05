using Microsoft.AspNetCore.Http;

namespace LoadBalancer.API.Route;

public interface IRouter
{
    Task RouteAsync(HttpContext context, string? targetUrl);
}