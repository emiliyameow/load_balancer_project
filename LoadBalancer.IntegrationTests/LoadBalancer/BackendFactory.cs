using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LoadBalancer.IntegrationTests.LoadBalancer;

public class BackendFactory : WebApplicationFactory<Backend.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("SERVER_NAME", "Server_1");
    }
}
