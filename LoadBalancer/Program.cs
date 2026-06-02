using LoadBalancer.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// регистрация конфигурации и сервисов
builder.AddLoadBalancerConfiguration();
builder.Services.AddLoadBalancerServices(builder.Configuration);

var app = builder.Build();

// настройка pipeline (middleware)
app.UseLoadBalancerPipeline();

app.Run();
