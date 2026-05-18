namespace LoadBalancer.API.Rout;

/// <summary>
/// Интерфейс для проксирования запроса на выбранный целевой сервер.
/// </summary>
public interface IRouter
{
    Task RouteAsync(HttpContext context, string? targetUrl);
}