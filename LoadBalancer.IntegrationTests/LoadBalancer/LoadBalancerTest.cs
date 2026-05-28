using FluentAssertions;
using LoadBalancer.API.Rout;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using IRouter = LoadBalancer.API.Rout.IRouter;
using Program = LoadBalancer.API.Program;

namespace LoadBalancer.IntegrationTests.LoadBalancer;

// ==================== ТЕСТ ====================
public class LoadBalancerTest : IClassFixture<LoadBalancerFactory>
{
    private readonly HttpClient _loadBalancer;

    public LoadBalancerTest(LoadBalancerFactory factory)
    {
        _loadBalancer = factory.CreateClient();
    }

    [Fact]
    public async Task RootEndpoint_ReturnsBackendResponse()
    {
        var response = await _loadBalancer.GetAsync("/");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();

        result.Should().Be("<div>Стартовая страница!</div>");
    }

    [Fact]
    public async Task TestEndpoint_ReturnsServerName()
    {
        var response = await _loadBalancer.GetAsync("/test");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();

        Assert.Equal("Server_1!", result);
    }
}