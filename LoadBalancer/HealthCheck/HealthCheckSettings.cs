using LoadBalancer.API.Rout;

namespace LoadBalancer.API.HealthCheck;

public class HealthCheckSettings
{
    public List<BackendConfig> Backends { get; set; } = [];

    public int IntervalSeconds { get; set; } = 10;
    public int TimeoutMilliseconds { get; set; } = 5000;
    public string HealthEndpoint { get; set; } = "/health";
}