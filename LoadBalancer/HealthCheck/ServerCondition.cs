using LoadBalancer.API.Rout;

namespace LoadBalancer.API.HealthCheck;

public class ServerCondition
{
    public bool IsAlive { get; set; }
    public int Weight { get; set; }
    public long? LatencyMs { get; set; }
    public DateTimeOffset? CheckedAt { get; set; }
    public string? Error { get; set; }
    public BackendConfig ServerInfo { get; set; } = new();
}
