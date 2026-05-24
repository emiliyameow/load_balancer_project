using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancer.IntegrationTests.LoadBalancer;

public class BackendFactory : WebApplicationFactory<Backend.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
    }
}
