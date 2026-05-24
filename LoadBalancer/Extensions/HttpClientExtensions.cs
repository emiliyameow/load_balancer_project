namespace LoadBalancer.API.Extensions;

using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.Rout;

/// <summary>
/// Регистрация HTTP клиентов.
/// </summary>
public static class HttpClientExtensions
{
    public static IServiceCollection AddLoadBalancerHttpClients(
        this IServiceCollection services)
    {
        // HTTP клиент для роутера
        services.AddHttpClient<IRouter, Router>()
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                // Настройки TCP соединений
                return new SocketsHttpHandler
                {
                    // периодический DNS re-resolve
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2),

                    // лимит соединений на backend
                    MaxConnectionsPerServer = 50,

                    // время жизни idle соединений
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
                };
            });

        // HTTP клиент для health check
        services.AddHttpClient<IHealthChecker, HealthChecker>();

        return services;
    }
}