namespace LoadBalancer.API.Api.DTO;

public class UpdateServerDTO
{
    public string ServiceName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? Host { get; set; }

    public int? Port { get; set; }

    public int? Weight { get; set; }
}