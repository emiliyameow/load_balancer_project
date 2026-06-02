using LoadBalancer.API.Rout;
using LoadBalancer.API.ServiceDiscovery;
using Microsoft.Extensions.Options;

namespace LoadBalancer.API.HealthCheck;

/// <summary>
/// Сервис для мониторинга состояния и загруженности фоновых серверов.
/// Выполняет опрос серверов и возвращает их текущие показатели для алгоритма балансировки.
/// </summary>
public class HealthChecker(
    HttpClient httpClient,
    IOptionsMonitor<HealthCheckSettings> settings,
    IServiceRegistry registry,
    ILogger<HealthChecker> logger) : IHealthChecker
{
    /// <summary>
    /// Асинхронно проверяет состояние всех серверов, указанных в конфигурации.
    /// </summary>
    /// <returns>
    /// Список объектов <see cref="ServerCondition"/>, содержащих статус (Alive) 
    /// и текущую нагрузку (Weight) каждого сервера.
    /// </returns>
    public async Task<List<ServerCondition>> CheckAllServersAsync()
    {
        var configuration = settings.CurrentValue;
        var services = await registry.GetServicesAsync();

        var servers = services
            .Values
            .SelectMany(serviceBackends => serviceBackends)
            .Select(server => server.ServerInfo)
            .GroupBy(server => server.Address, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (servers.Count == 0)
        {
            logger.LogWarning("Фоновые серверы не настроены");
            return new List<ServerCondition>();
        }

        var tasks = servers.Select(async server => await CheckServerAsync(
            server,
            configuration.HealthEndpoint,
            configuration.TimeoutMilliseconds));

        var resultsArray = await Task.WhenAll(tasks);

        return resultsArray.ToList();
    }

    /// <summary>
    /// Выполняет проверку конкретного сервера и получает данные о его загруженности.
    /// </summary>
    private async Task<ServerCondition> CheckServerAsync(BackendConfig server, string endpoint, int timeoutMs)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var condition = new ServerCondition
        {
            ServerInfo = server,
            IsAlive = false,
            Weight = 0,
            CheckedAt = startedAt
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
        try
        {
            var requestUri = $"{server.Address.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            using var response = await httpClient.GetAsync(requestUri, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cts.Token);

                condition.IsAlive = true;
                if (TryParseWeight(content, out var weight)) condition.Weight = weight;
            }
            else
            {
                condition.Error = $"{(int)response.StatusCode} {response.ReasonPhrase}".Trim();
            }
        }
        catch (OperationCanceledException)
        {
            condition.Error = "Health check timed out";
            logger.LogTrace("Превышено время ожидания проверки состояния для {Address}", server.Address);
        }
        catch (Exception ex)
        {
            condition.Error = ex.Message;
            logger.LogWarning(ex, "Ошибка при проверке состояния сервера {Address}", server.Address);
        }
        finally
        {
            stopwatch.Stop();
            condition.LatencyMs = stopwatch.ElapsedMilliseconds;
        }

        return condition;
    }

    private static bool TryParseWeight(string content, out int weight)
    {
        return int.TryParse(content.Trim().Trim('"'), out weight);
    }
}
