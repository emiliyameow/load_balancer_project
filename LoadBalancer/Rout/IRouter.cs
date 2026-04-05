using Microsoft.AspNetCore.Http;

namespace LoadBalancer.API.Rout;
public interface IRouter
{
    Task RouteAsync(HttpContext context, string? targetUrl);
}