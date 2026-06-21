using System.Text;
using System.Text.Json.Serialization;
using ChatSphere.Api.Auth;
using ChatSphere.Api.Demo;
using ChatSphere.Api.Hubs;
using ChatSphere.Api.Middleware;
using ChatSphere.Application.Common.Interfaces;
using ChatSphere.Infrastructure;
using ChatSphere.Infrastructure.Auth;
using ChatSphere.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// SignalR + Redis backplane (only when Redis is reachable, so dev works without it).
var signalR = builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var redisConn = builder.Configuration.GetConnectionString("Redis") ?? builder.Configuration["Redis"];
using (var probe = ChatSphere.Infrastructure.DependencyInjection.TryConnectRedis(builder.Configuration))
{
    if (probe is not null && !string.IsNullOrWhiteSpace(redisConn))
        signalR.AddStackExchangeRedis(redisConn);
}

var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwt.Secret))
    throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        // WebSockets can't send Authorization headers — read the token from the query string for hub paths.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

// Ambient "live" activity so the demo feels alive for a solo visitor.
if (builder.Configuration.GetValue("DemoActivity", true))
    builder.Services.AddHostedService<DemoActivityService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
     .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatSphereDbContext>();
    await db.Database.MigrateAsync();
    if (app.Configuration.GetValue("SeedDemoData", true))
        await DataSeeder.SeedAsync(db, DateTimeOffset.UtcNow);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "chatsphere-api" }));
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

public partial class Program;
