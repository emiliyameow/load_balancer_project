using LoadBalancer.API.Balance;
using LoadBalancer.API.BackendManagement;
using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.ServiceCache;
using LoadBalancer.API.ServiceDiscovery;

namespace LoadBalancer.API.Extensions;
/// <summary>
/// Регистрация всех сервисов приложения.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLoadBalancerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // HTTP клиенты
        services.AddLoadBalancerHttpClients();

        // HealthCheck настройки
        services.Configure<HealthCheckSettings>(settings =>
        {
            configuration.GetSection("Settings:Backends")
                .Bind(settings.Backends);

            configuration.GetSection("Settings:HealthCheck")
                .Bind(settings);
        });

        // глобальные настройки
        services.Configure<Settings>(
            configuration.GetSection("Settings"));

        // кеш сервисов
        services.AddSingleton<ServiceCacheHandler>();
        services.AddSingleton<HealthCache>();

        // балансировка
        services.AddSingleton<IBalanceStrategy, MinWeightStrategy>();
        services.AddSingleton<IBalanceStrategy, WeightedRoundRobinStrategy>();

        services.AddSingleton<BalanceStrategyRegistry>();
        services.AddSingleton<BalanceAlgorithm>();
        services.AddSingleton<BackendLoadTracker>();

        // discovery — RuntimeBackendRegistry хранит как config, так и runtime-добавленные серверы
        services.AddSingleton<RuntimeBackendRegistry>();
        services.AddSingleton<IServiceRegistry>(
            sp => sp.GetRequiredService<RuntimeBackendRegistry>());

        // hosted services
        services.AddHostedService<HealthCheckHostedService>();
        services.AddHostedService<ServiceDiscoveryUpdater>();

        // MVC
        services.AddControllers();

        return services;
    }
}
