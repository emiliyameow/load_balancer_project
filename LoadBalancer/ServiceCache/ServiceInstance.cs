namespace LoadBalancer.API.ServiceCache;

public record ServiceInstance(
    string Id,        // уникальный id (host:port)
    string Host,
    int Port,
    int Weight = 1,
    string? Version = null
);