using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.ServiceCache;
using System.Collections.Immutable;
using System.Threading;


namespace LoadBalancer.API.ServiceCache;

/// <summary>
/// Потокобезопасный кэш сервисов на основе immutable snapshot.
/// Обеспечивает быстрые чтения и атомарные обновления.
/// </summary>
public class ServiceCacheHandler
{
    // Текущий snapshot (никогда не мутируется, только заменяется целиком)
    private ImmutableDictionary<string, ImmutableList<ServerCondition>> _cache
        = ImmutableDictionary<string, ImmutableList<ServerCondition>>.Empty;

    /// <summary>
    /// Возвращает список инстансов для указанного сервиса.
    /// </summary>
    public IReadOnlyList<ServerCondition> GetInstances(string serviceName)
    {
        // читаем snapshot один раз (важно для thread-safety)
        var snapshot = _cache;

        return snapshot.TryGetValue(serviceName, out var instances)
            ? instances
            : ImmutableList<ServerCondition>.Empty;
    }

    /// <summary>
    /// Возвращает полный snapshot всех сервисов.
    /// </summary>
    public IReadOnlyDictionary<string, ImmutableList<ServerCondition>> GetAll()
    {
        return _cache;
    }

    /// <summary>
    /// Атомарно добавляет или обновляет сервис (CAS loop).
    /// </summary>
    public void AddOrUpdateService(string service, ImmutableList<ServerCondition> instances)
    {
        while (true)
        {
            // читаем текущий snapshot
            var snapshot = _cache;

            // создаём НОВЫЙ snapshot с обновлённым сервисом
            var updated = snapshot.SetItem(service, instances);

            // пытаемся атомарно заменить snapshot
            var original = Interlocked.CompareExchange(ref _cache, updated, snapshot);

            // если snapshot не изменился другим потоком — успех
            if (ReferenceEquals(original, snapshot))
                return;
            
            // иначе кто-то изменил cache → повторяем попытку
        }
    }
    /// <summary>
    /// Атомарно обновляет параметры сервера внутри сервиса.
    /// Сервер ищется по serviceName + serverName.
    /// </summary>
    public bool UpdateServer(
        string serviceName,
        string serverName,
        string? address,
        string? host,
        int? port,
        int? weight)
    {
        while (true)
        {
            var snapshot = _cache;

            if (!snapshot.TryGetValue(serviceName, out var instances))
                return false;

            var serverIndex = instances.FindIndex(x =>
                x.ServerInfo.Name == serverName
            );

            if (serverIndex == -1)
                return false;

            var oldServer = instances[serverIndex];

            var updatedServerInfo = new ServerInfo
            {
                Name = oldServer.ServerInfo.Name,
                Address = address ?? oldServer.ServerInfo.Address,
                Host = host ?? oldServer.ServerInfo.Host,
                Port = port ?? oldServer.ServerInfo.Port
            };

            var updatedServer = new ServerCondition
            {
                ServerInfo = updatedServerInfo,
                IsAlive = oldServer.IsAlive,
                Weight = weight ?? oldServer.Weight
            };

            var updatedInstances = instances.SetItem(serverIndex, updatedServer);

            var updatedSnapshot = snapshot.SetItem(serviceName, updatedInstances);

            var original = Interlocked.CompareExchange(
                ref _cache,
                updatedSnapshot,
                snapshot
            );

            if (ReferenceEquals(original, snapshot))
                return true;
        }
    }
}