using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace LoadBalancer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();


        app.Map("/{**path}", async (
            HttpContext context
            ,string path
            ) =>
        {
            var backendsConfig = builder.Configuration
            .GetSection("Settings:Backends")
            .Get<List<BackendConfig>>();

            var server_1 = backendsConfig?.FirstOrDefault();
            if (server_1 == null)
            {
                context.Response.StatusCode = 503;
                await context.Response.WriteAsync("No backend configured");
                return;
            }

            var targetUrl = $"{server_1.Address}/{path}{context.Request.QueryString}";

            using var client = new HttpClient();
            var response = await client.GetAsync(targetUrl);
            await response.Content.CopyToAsync(context.Response.Body);
        });


        app.Run();
    }
}

public class BackendConfig
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty ;

    // ”добное свойство дл€ получени€ полного адреса
    public string Address => $"http://{Host}:{Port}";
}