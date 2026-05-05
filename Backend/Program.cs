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

        app.MapGet("/health", () => Results.Ok(_activeRequestsCounter.ToString()));

        app.MapGet("/test", () => Task.FromResult("Server_1!"));
        app.MapGet("/", () => Task.FromResult("<div>Стартовая страница!</div>"));

        app.Run();
    }
}