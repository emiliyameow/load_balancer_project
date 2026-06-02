namespace Backend;

public class Program
{
    private static int _activeRequestsCounter;
    
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        // Middleware для подсчета загруженности
        app.Use(async (context, next) =>
        {
            // Исключаем endpoint /health из подсчета нагрузки
            if (context.Request.Path == "/health")
            {
                await next();
                return;
            }

            // Пришел запрос: +1 к весу
            Interlocked.Increment(ref _activeRequestsCounter);
            try
            {
                await next();
            }
            finally
            {
                // Запрос обработан: -1 к весу
                Interlocked.Decrement(ref _activeRequestsCounter);
            }
        });

        app.MapGet("/health", () => Results.Ok(_activeRequestsCounter));

        app.MapGet("/test", async (HttpContext context, IConfiguration configuration) =>
        {
            var delayMs = 0;
            if (context.Request.Query.TryGetValue("delayMs", out var rawDelay) &&
                int.TryParse(rawDelay, out var parsedDelay))
            {
                delayMs = Math.Clamp(parsedDelay, 0, 30_000);
            }

            if (delayMs > 0)
                await Task.Delay(delayMs, context.RequestAborted);

            var serverName = configuration["SERVER_NAME"];
            if (string.IsNullOrWhiteSpace(serverName))
                serverName = context.Connection.LocalPort > 0
                    ? $"Server_{context.Connection.LocalPort}"
                    : "Server";

            return $"{serverName}!";
        });
        app.MapGet("/", () => Task.FromResult("<div>Стартовая страница!</div>"));

        app.Run();
    }
}
