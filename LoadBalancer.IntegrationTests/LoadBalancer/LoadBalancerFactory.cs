using LoadBalancer.API;
using LoadBalancer.API.Rout;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancer.IntegrationTests.LoadBalancer;

public class LoadBalancerFactory : WebApplicationFactory<Program>
{
    public readonly BackendFactory BackendFactory;
    private readonly HttpMessageHandler _backendHandler;

    public LoadBalancerFactory()
    {
        BackendFactory = new BackendFactory();

        // Получаем HttpMessageHandler от тестового сервера бэкенда
        // Этот хендлер позволяет делать "in-memory" запросы к бэкенду
        _backendHandler = BackendFactory.Server.CreateHandler();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {

        // 2. Заменяем HttpClient для Router на клиент с тестовым хендлером
        builder.ConfigureServices(services =>
        {
            // Находим и удаляем все регистрации HttpClient для IRouter
            var descriptors = services
                .Where(d => d.ServiceType == typeof(IRouter))
                .ToList();

            foreach (var desc in descriptors)
            {
                services.Remove(desc);
            }

            // Создаём HttpClient, который использует handler от BackendFactory.Server
            var backendClient = new HttpClient(_backendHandler)
            {
                BaseAddress = new Uri("http://localhost") // базовый адрес не важен, хендлер перехватит запрос
            };

            // Регистрируем Router с нашим тестовым HttpClient
            services.AddSingleton<IRouter>(_ => new Router(backendClient));

            // Отключаем фоновые health-check запросы (чтобы не было гонок в тестах)
            //var hcDescriptor = services.SingleOrDefault(
            //    d => d.ImplementationType == typeof(LoadBalancer.API.HealthCheck.HealthCheckHostedService));
            //if (hcDescriptor != null)
            //    services.Remove(hcDescriptor);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _backendHandler?.Dispose();
            BackendFactory?.Dispose();
        }
        base.Dispose(disposing);
    }
}
