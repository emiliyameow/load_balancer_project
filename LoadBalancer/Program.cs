namespace LoadBalancer.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddHttpClient<IRouter, Router>();
        var app = builder.Build();

        app.Map("/{**path}", async (HttpContext context, IRouter router, IConfiguration configuration) =>
        {
            var backends = configuration
                .GetSection("Settings:Backends")
                .Get<List<BackendConfig>>();

            var backend = backends?.FirstOrDefault();
            

            await router.RouteAsync(context, backend.Address);
        });

        app.Run();
    }
}