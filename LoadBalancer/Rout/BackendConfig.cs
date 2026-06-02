namespace LoadBalancer.API.Rout;

public class BackendConfig
{
    public string Name { get; set; } = string.Empty;
    public string Scheme { get; set; } = "http";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public int Weight { get; set; } = 1;

    /// <summary>
    /// Формирует полный адрес: {Scheme}://{Host}:{Port}
    /// </summary>
    public string Address => $"{Scheme}://{Host}:{Port}";
}
