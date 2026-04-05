using LoadBalancer.API.Route;
using LoadBalancer.API.ServiceCache;
using System.Collections.Immutable;
using IRouter = LoadBalancer.API.Route.IRouter;

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

var app = builder.Build();

app.UseMiddleware<RoutingMiddleware>();

// пример использования service cache
var cache = new ServiceCache();

// создаём snapshot
var snapshot = ImmutableDictionary<string, ImmutableList<ServiceInstance>>
    .Empty
    .Add("users-service", ImmutableList.Create(
        new ServiceInstance("1", "10.0.0.1", 8080),
        new ServiceInstance("2", "10.0.0.2", 8080)
    ));

// обновляем
cache.UpdateSnapshot(snapshot);

// читаем
var instances = cache.GetInstances("users-service");

foreach (var i in instances)
{
    Console.WriteLine($"{i.Host}:{i.Port}");
}

app.Run();