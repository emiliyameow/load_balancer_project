using LoadBalancer.API.Rout;

namespace LoadBalancer.API.ServiceDiscovery;

public class Settings
{
    public List<BackendConfig> Backends { get; set; } = new();
}