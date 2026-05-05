using FluentAssertions;
using LoadBalancer.API.HealthCheck;
using Microsoft.Extensions.DependencyInjection;

namespace LoadBalancer.IntegrationTests.Health;

public class HealthTest : IClassFixture<LoadBalancerFactory>
{
    private readonly LoadBalancerFactory _factory;
    private readonly IHealthChecker _healthChecker;

    public HealthTest(LoadBalancerFactory factory)
    {
        _factory = factory;
        // Получаем HealthChecker из DI-контейнера фабрики
        _healthChecker = factory.Services.GetRequiredService<IHealthChecker>();
    }

    [Fact]
    public async Task HealthCheck_AllLives()
    {
        _factory.InitBackends(2);
        // Arrange: все бэкенды живы, вес = 0
        var backend0 = _factory.GetBackend("server-0");
        var backend1 = _factory.GetBackend("server-1");

        backend0?.SetEnableStatus(true);
        backend1?.SetEnableStatus(true);
        backend0?.SetWeight(5);
        backend1?.SetWeight(10);

        // Act
        var results = await _healthChecker.CheckAllServersAsync();

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.ServerInfo.Name == "server-0" && r.IsAlive && r.Weight == 5);
        results.Should().Contain(r => r.ServerInfo.Name == "server-1" && r.IsAlive && r.Weight == 10);
    }

    [Fact]
    public async Task HealthCheck_OneNotAlive()
    {
        _factory.InitBackends(2);
        // Arrange: все бэкенды живы, вес = 0
        var backend0 = _factory.GetBackend("server-0");
        var backend1 = _factory.GetBackend("server-1");

        backend0?.SetEnableStatus(true);
        backend1?.SetEnableStatus(false);
        backend0?.SetWeight(5);
        backend1?.SetWeight(10);

        // Act
        var results = await _healthChecker.CheckAllServersAsync();
        var aliveBackends = results.Where(r => r.IsAlive).ToList();

        // Assert
        //results.Should().HaveCount(2);
        //results.Should().Contain(r => r.ServerInfo.Name == "server-0" && r.IsAlive && r.Weight == 5);
        //results.Should().Contain(r => r.ServerInfo.Name == "server-1" && !r.IsAlive && r.Weight == 10);
        aliveBackends.Should().HaveCount(1);
    }
}