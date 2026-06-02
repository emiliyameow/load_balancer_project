using System.Collections.Concurrent;

namespace LoadBalancer.API.HealthCheck;

public readonly record struct ServerHealth(
    bool IsAlive,
    int Weight,
    long? LatencyMs = null,
    DateTimeOffset? CheckedAt = null,
    string? Error = null);

/// <summary>
/// Хранит именно адрес сервера + его состояние
/// </summary>
public class HealthCache
{
    // key = адрес сервера
    private readonly ConcurrentDictionary<string, ServerHealth> _health = new();

    public void Update(IEnumerable<ServerCondition> servers)
    {
        foreach (var server in servers)
        {
            var key = server.ServerInfo.Address;
            _health[key] = new ServerHealth(
                server.IsAlive,
                server.Weight,
                server.LatencyMs,
                server.CheckedAt,
                server.Error);
        }
    }

    public void Upsert(string address, ServerHealth health)
    {
        _health[address] = health;
    }

    public void Remove(string address)
    {
        _health.TryRemove(address, out _);
    }

    public bool TryGet(string address, out ServerHealth health)
    {
        return _health.TryGetValue(address, out health);
    }

    public bool IsHealthy(string address)
    {
        return _health.TryGetValue(address, out var health) && health.IsAlive;
    }
}
