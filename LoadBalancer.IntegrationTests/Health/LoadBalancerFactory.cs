using LoadBalancer.API;
using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.Rout;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancer.IntegrationTests.Health;

public class LoadBalancerFactory : WebApplicationFactory<Program>
{
    private readonly List<BackendFactory> _backendFactories = new();
    private readonly Dictionary<string, HttpClient> _backendHandlers = new();

    public void InitBackends(int amount = 2)
    {
        foreach (var handler in _backendHandlers.Values) handler.Dispose();
        foreach (var backend in _backendFactories) backend.Dispose();

        _backendFactories.Clear();
        _backendHandlers.Clear();

        for (int i = 0; i < amount; i++)
        {
            var backend = new BackendFactory { Name = $"server-{i}" };
            _backendFactories.Add(backend);

            // 🔑 Ключевое: берём HttpMessageHandler, а не HttpClient!
            _backendHandlers[backend.Name] = backend.CreateClient();
        }
    }

    public BackendFactory GetBackend(string name) =>
        _backendFactories.FirstOrDefault(b => b.Name == name);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        InitBackends();

        builder.ConfigureServices(services =>
        {
            // 1. Настраиваем тестовые настройки с фиктивными адресами
            var testSettings = new HealthCheckSettings
            {
                HealthEndpoint = "/health",
                TimeoutMilliseconds = 5000,
                Backends = _backendFactories.Select(b => new BackendConfig
                {
                    Name = b.Name,
                    Host = b.Name,          // server-0, server-1...
                    Port = 80,              // не важно, хендлер проигнорирует
                }).ToList()
            };

            // 2. Мокаем IOptionsMonitor<HealthCheckSettings>
            var optionsMonitorMock = new Mock<IOptionsMonitor<HealthCheckSettings>>();
            optionsMonitorMock.Setup(x => x.CurrentValue).Returns(testSettings);
            optionsMonitorMock
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns(testSettings);

            // 3. Удаляем оригинальные регистрации
            RemoveService<HealthCheckSettings>(services);
            RemoveService<IHealthChecker>(services);
            RemoveService<IOptionsMonitor<HealthCheckSettings>>(services);

            // 4. Создаём мульти-хендлер и клиент
            var pipeline = new BackendNameHeaderHandler { InnerHandler = new MultiBackendHandler(_backendHandlers) };
            var healthClient = new HttpClient(pipeline, disposeHandler: false);

            // 5. Регистрируем HealthChecker с моками
            services.AddSingleton(optionsMonitorMock.Object);
            services.AddSingleton<IHealthChecker>(_ =>
                new HealthChecker(healthClient, optionsMonitorMock.Object, NullLogger<HealthChecker>.Instance));
        });
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var d in descriptors) services.Remove(d);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var handler in _backendHandlers.Values) handler.Dispose();
            foreach (var backend in _backendFactories) backend.Dispose();
        }
        base.Dispose(disposing);
    }
}
