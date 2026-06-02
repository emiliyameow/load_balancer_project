namespace LoadBalancer.API.Api.DTO;

public class ServerDTO
{
    public string Name { get; set; } =  string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public int Port { get; set; }

    public string Host { get; set; } = string.Empty;
    
    public bool IsAlive { get; set; }
    
    public int Weight { get; set; }

    public int BalancerActiveRequests { get; set; }

    public int EffectiveWeight { get; set; }

    public long? LatencyMs { get; set; }

    public DateTimeOffset? CheckedAt { get; set; }

    public string? Error { get; set; }
}
