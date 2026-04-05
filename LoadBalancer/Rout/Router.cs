using Microsoft.AspNetCore.Http.Features;

namespace LoadBalancer.API.Rout;
public class Router : IRouter
{
    private readonly HttpClient _httpClient;

    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
        "Proxy-Connection"
    };

    public Router(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task RouteAsync(HttpContext context, string targetUrl)
    {
        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Backend URL is not configured");
            return;
        }

        var request = context.Request;
        var baseUri = new Uri(targetUrl);

        var builder = new UriBuilder(baseUri)
        {
            Path = $"{baseUri.AbsolutePath.TrimEnd('/')}/{request.Path.Value?.TrimStart('/')}",
            Query = request.QueryString.HasValue ? request.QueryString.Value!.TrimStart('?') : string.Empty
        };

        var destinationUri = builder.Uri;

        using var proxyRequest = new HttpRequestMessage(new HttpMethod(request.Method), destinationUri);

        // Проверяем через IHttpRequestBodyDetectionFeature, может ли запрос содержать тело.
        // Это круче, чем вручную проверять Content-Length и Transfer-Encoding.
        if (request.HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody == true)
        {
            // Обертка нужна, чтобы StreamContent не закрыл request.Body.
            proxyRequest.Content = new StreamContent(new NonClosingStream(request.Body));
        }

        foreach (var header in request.Headers)
        {
            if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase))
                continue;

            if (HopByHopHeaders.Contains(header.Key))
                continue;

            if (!proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                proxyRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(remoteIp))
        {
            if (request.Headers.TryGetValue("X-Forwarded-For", out var existingFor))
                proxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-For", $"{existingFor}, {remoteIp}");
            else
                proxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-For", remoteIp);
        }

        proxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-Proto", request.Scheme);
        proxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-Host", request.Host.Value);

        proxyRequest.Headers.Host = proxyRequest.RequestUri?.Host;

        try
        {
            using var response = await _httpClient.SendAsync(
                proxyRequest,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);

            context.Response.StatusCode = (int)response.StatusCode;

            foreach (var header in response.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();

            foreach (var header in response.Content.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();

            context.Response.Headers.Remove("transfer-encoding");

            await response.Content.CopyToAsync(context.Response.Body);
        }
        catch (TaskCanceledException)
        {
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            await context.Response.WriteAsync("Request to backend timed out");
        }
        catch (HttpRequestException)
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await context.Response.WriteAsync("Backend is unavailable");
        }
    }
}