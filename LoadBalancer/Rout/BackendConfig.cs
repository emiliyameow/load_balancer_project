namespace LoadBalancer.API.Rout;

public class BackendConfig
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public int Weight { get; set; } = 1;

    /// <summary>
    /// Формирует полный адресс: http://{Host}:{Port}
    /// </summary>
    public string Address => $"http://{Host}:{Port}";
}
