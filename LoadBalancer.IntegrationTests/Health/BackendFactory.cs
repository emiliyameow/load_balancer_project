using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LoadBalancer.IntegrationTests.Health;

public class BackendFactory : WebApplicationFactory<Backend.Program>
{
    private bool _enabled = true;
    public string Name { get; set; }
    private int _weight = 0; // можно динамически менять нагрузку


    public void SetEnableStatus(bool isAlive) => _enabled = isAlive;
    public void SetWeight(int weight) => _weight = weight;


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.Configure(app =>
        {
            // Перехватываем все запросы
            app.Use(next =>
            {
                return async context =>
                {
                    if (context.Request.Path != "/health")
                    {
                        await next(context);
                        return;
                    }

                    if (!_enabled)
                    {
                        // Имитируем недоступность сервиса
                        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                        await context.Response.WriteAsync("Backend is down");
                        return;
                    }

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync(_weight.ToString());
                };
            });
        });
    }
}
