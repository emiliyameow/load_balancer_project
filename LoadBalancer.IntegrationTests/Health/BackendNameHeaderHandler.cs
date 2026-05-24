using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancer.IntegrationTests.Health;

public class BackendNameHeaderHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Извлекаем имя из http://server-0/health → server-0
        if (request.RequestUri?.Host is { } host && !string.IsNullOrEmpty(host))
        {
            request.Headers.Add("X-Backend-Name", host);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
