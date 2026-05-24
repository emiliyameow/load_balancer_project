using LoadBalancer.API.Balance;
using LoadBalancer.API.Rout;
using LoadBalancer.API.ServiceCache;
using System.Collections.Immutable;
using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.ServiceDiscovery;
using IRouter = LoadBalancer.API.Rout.IRouter;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHttpClient<IRouter, Router>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new SocketsHttpHandler
        {
            // Обновляем соединения, чтобы клиент периодически заново резолвил DNS
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),

            // Ограничение числа соединений на один backend
            MaxConnectionsPerServer = 50,

            // Сколько неиспользуемое соединение может жить в пуле
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        };
    });
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.Configure<HealthCheckSettings>(settings =>
{
    builder.Configuration.GetSection("Settings:Backends").Bind(settings.Backends);
    builder.Configuration.GetSection("Settings:HealthCheck").Bind(settings);
});

builder.Services.AddSingleton<IHealthChecker, HealthChecker>();
builder.Services.AddHttpClient<IHealthChecker, HealthChecker>();

// добавляем синглтон - кэш
builder.Services.AddSingleton<ServiceCacheHandler>();
// стратегии балансировки
builder.Services.AddSingleton<IBalanceStrategy, MinWeightStrategy>();
builder.Services.AddSingleton<IBalanceStrategy, WeightedRoundRobinStrategy>();

// реестр стратегий
builder.Services.AddSingleton<BalanceStrategyRegistry>();

// балансировщик
builder.Services.AddSingleton<BalanceAlgoritm>();
// добавляем фоновую службу HealtCheck
builder.Services.AddHostedService<HealthCheckHostedService>();
// добавляем синглтон - хелф кэш
builder.Services.AddSingleton<HealthCache>();

builder.Services.Configure<Settings>(
    builder.Configuration.GetSection("Settings"));

builder.Services.AddSingleton<IServiceRegistry, FakeServiceRegistry>();

builder.Services.AddHostedService<ServiceDiscoveryUpdater>();

builder.Services.AddControllers();


var app = builder.Build();

app.MapControllers();


app.UseMiddleware<RoutingMiddleware>();
// перенесла создание снэпшота в Service Discovery Updater
app.Run();