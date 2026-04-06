using LoadBalancer.API.Rout;

namespace LoadBalancer.API.HealthCheck;

public class ServerCondition
{
    public bool IsAlive { get; set; }
    public int Weight { get; set; }
    public BackendConfig ServerInfo { get; set; }
}