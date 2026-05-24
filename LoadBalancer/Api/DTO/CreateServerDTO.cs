namespace LoadBalancer.API.Api.DTO;

public class CreateServerDTO
{
    public string ServiceName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public int Port { get; set; }

    public int Weight { get; set; } = 1;
}
