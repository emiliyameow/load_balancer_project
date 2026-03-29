namespace Backend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/test", () => "Server_1!");

        app.Run();
    }
}
