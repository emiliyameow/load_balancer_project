namespace LoadBalancer.API;

public class Router : IRouter
{
    private readonly HttpClient _httpClient;

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
        var targetUri = $"{targetUrl.TrimEnd('/')}{request.Path}{request.QueryString}";

        using var proxyRequest = new HttpRequestMessage(new HttpMethod(request.Method), targetUri);

        if (request.ContentLength is > 0)
        {
            proxyRequest.Content = new StreamContent(request.Body);
        }

        foreach (var header in request.Headers)
        {
            if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                proxyRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        proxyRequest.Headers.Host = proxyRequest.RequestUri?.Host;

        try
        {
            using var response = await _httpClient.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

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