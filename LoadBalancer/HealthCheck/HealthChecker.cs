using LoadBalancer.API.Rout;
using Microsoft.Extensions.Options;

namespace LoadBalancer.API.HealthCheck;

/// <summary>
/// Сервис для мониторинга состояния и загруженности фоновых серверов.
/// Выполняет опрос серверов и возвращает их текущие показатели для алгоритма балансировки.
/// </summary>
public class HealthChecker(
    HttpClient httpClient,
    IOptionsMonitor<HealthCheckSettings> settings,
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

        var servers = configuration.Backends;
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
        var condition = new ServerCondition
        {
            ServerInfo = server,
            IsAlive = false,
            Weight = 0
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
        try
        {
            var requestUri = $"{server.Address.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var response = await httpClient.GetAsync(requestUri, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cts.Token);

                condition.IsAlive = true;
                if (int.TryParse(content, out var weight)) condition.Weight = weight;
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogTrace("Превышено время ожидания проверки состояния для {Address}", server.Address);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка при проверке состояния сервера {Address}", server.Address);
        }

        return condition;
    }
}