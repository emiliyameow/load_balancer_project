using LoadBalancer.API;
using IRouter = LoadBalancer.API.IRouter;

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

app.Run();