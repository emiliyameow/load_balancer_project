namespace LoadBalancer.API.HealthCheck;

public interface IHealthChecker
{
    Task<List<ServerCondition>> CheckAllServersAsync();
}