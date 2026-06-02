using LoadBalancer.API.BackendManagement;
using LoadBalancer.API.Rout;

namespace LoadBalancer.API.Extensions;

/// <summary>
/// Конфигурация middleware pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Настройка HTTP pipeline приложения.
    /// </summary>
    public static WebApplication UseLoadBalancerPipeline(
        this WebApplication app)
    {
        // маршрутизация контроллеров
        app.MapControllers();

        // Minimal API для runtime-управления backend-серверами
        app.MapBackendApi();

        // основной middleware балансировки/роутинга
        app.UseMiddleware<RoutingMiddleware>();

        return app;
    }
}
