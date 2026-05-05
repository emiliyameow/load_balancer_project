namespace LoadBalancer.API.Api.DTO;

public class ServerDTO
{
    public string Name { get; set; } =  string.Empty;

    public string ServiceName { get; set; }

    public string Address { get; set; }

    public int Port { get; set; }

    public string Host { get; set; }
    
    public bool IsAlive { get; set; }
    
    public int Weight { get; set; }
}