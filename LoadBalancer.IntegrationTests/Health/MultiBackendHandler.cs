using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancer.IntegrationTests.Health;

public class MultiBackendHandler : HttpMessageHandler
{
    private readonly Dictionary<string, HttpClient> _backendHandlers;

    public MultiBackendHandler(Dictionary<string, HttpClient> backendHandlers)
    {
        _backendHandlers = backendHandlers;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 🔑 Маршрутизация по кастомному заголовку
        if (!request.Headers.TryGetValues("X-Backend-Name", out var backendNames))
        {
            return new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("Missing X-Backend-Name header")
            };
        }

        var backendName = backendNames.FirstOrDefault();

        if (string.IsNullOrEmpty(backendName) || !_backendHandlers.TryGetValue(backendName, out var handler))
        {
            return new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent($"Backend '{backendName}' not found")
            };
        }

        // Клонируем запрос (HttpRequestMessage нельзя переиспользовать)
        var clonedRequest = await CloneRequestAsync(request);

        // Удаляем наш служебный заголовок, чтобы не "утек" в бэкенд
        clonedRequest.Headers.Remove("X-Backend-Name");

        return await handler.SendAsync(clonedRequest, cancellationToken);
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version,
            VersionPolicy = original.VersionPolicy
        };

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (original.Content != null)
        {
            var memoryStream = new MemoryStream();
            await original.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            clone.Content = new StreamContent(memoryStream);

            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
