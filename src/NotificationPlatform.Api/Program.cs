using NotificationPlatform.Api.Middleware;
using NotificationPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Notification Platform API", Version = "v1" });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();

// Catalog + all tenant DB migrations run via TenantMigrationRunner (IHostedService)
app.Run();

public partial class Program { }
