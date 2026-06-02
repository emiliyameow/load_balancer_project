namespace LoadBalancer.API.BackendManagement;

public sealed record ServerDto(
    string Address,
    int Port,
    bool IsAlive,
    string Name,
    string Host,
    string ServiceName,
    int Weight,
    int BalancerActiveRequests,
    int EffectiveWeight,
    long? LatencyMs,
    DateTimeOffset? CheckedAt,
    string? Error);

public sealed class CreateServerDto
{
    public string ServiceName { get; init; } = string.Empty;
    public string? Address { get; init; }
    public int Port { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Host { get; init; } = string.Empty;
    public int Weight { get; init; } = 1;
}

public sealed class UpdateServerDto
{
    public string ServiceName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Host { get; init; }
    public int? Port { get; init; }
    public int? Weight { get; init; }
}
