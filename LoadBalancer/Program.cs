using LoadBalancer.API.Balance;
using LoadBalancer.API.Rout;
using LoadBalancer.API.ServiceCache;
using System.Collections.Immutable;
using LoadBalancer.API.HealthCheck;
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

builder.Services.Configure<HealthCheckSettings>(settings =>
{
    builder.Configuration.GetSection("Settings:Backends").Bind(settings.Backends);
    builder.Configuration.GetSection("Settings:HealthCheck").Bind(settings);
});

builder.Services.AddSingleton<IHealthChecker, HealthChecker>();
builder.Services.AddHttpClient<IHealthChecker, HealthChecker>();

// добавляем синглтон - кэш
builder.Services.AddSingleton<ServiceCacheHandler>();
// добавляем синглтон - балансировщик
builder.Services.AddSingleton<BalanceAlgoritm>();
// добавляем фоновую службу HealtCheck
builder.Services.AddHostedService<HealthCheckHostedService>();

var app = builder.Build();

app.UseMiddleware<RoutingMiddleware>();

// создаём snapshot
InitialSnapshot(app);

app.Run();

static void InitialSnapshot(WebApplication app)
{
    var cache = app.Services.GetService<ServiceCacheHandler>();
    var backendsConfig = app.Configuration
            .GetSection("Settings:Backends")
            .Get<List<BackendConfig>>()
            .Select(b => new ServerCondition
            {
                IsAlive = true,
                Weight = 0,
                ServerInfo = new BackendConfig
                {
                    Name = b.Name,
                    Port = b.Port,
                    Host = b.Host,
                }
            })
            .ToImmutableList();

    var snapshot = ImmutableDictionary<string, ImmutableList<ServerCondition>>
        .Empty
        .Add("users-service",
        backendsConfig);

    // обновляем кэш (добавляем список всех серверов из конфига)
    cache.UpdateSnapshot(snapshot);
}