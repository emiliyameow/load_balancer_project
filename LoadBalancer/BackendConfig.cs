namespace LoadBalancer;

public class BackendConfig
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty ;

    // ������� �������� ��� ��������� ������� ������
    public string Address => $"http://{Host}:{Port}";
}