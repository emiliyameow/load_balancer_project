namespace LoadBalancer.API.Extensions;

/// <summary>
/// Расширения для конфигурации приложения.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Подключает конфигурационные файлы приложения.
    /// </summary>
    public static WebApplicationBuilder AddLoadBalancerConfiguration(
        this WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile(
            "appsettings.json",
            optional: false,
            reloadOnChange: true);

        builder.Configuration.AddEnvironmentVariables();

        return builder;
    }
}
