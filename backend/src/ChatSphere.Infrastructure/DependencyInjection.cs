using ChatSphere.Application;
using ChatSphere.Application.Common;
using ChatSphere.Application.Common.Interfaces;
using ChatSphere.Infrastructure.Auth;
using ChatSphere.Infrastructure.Common;
using ChatSphere.Infrastructure.Data;
using ChatSphere.Infrastructure.Presence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ChatSphere.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("Default")
                 ?? "Server=localhost;Database=ChatSphere;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<ChatSphereDbContext>(opt => opt.UseSqlServer(cs, sql => sql.EnableRetryOnFailure(3)));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<ChatSphereDbContext>());

        services.AddSingleton<IClock, SystemClock>();

        services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));
        services.Configure<AuthSettings>(config.GetSection(AuthSettings.SectionName));

        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<ITokenHasher, TokenHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Presence: Redis when reachable (correct across instances), else an in-process fallback.
        var mux = TryConnectRedis(config);
        if (mux is not null)
        {
            services.AddSingleton(mux);
            services.AddSingleton<IPresenceTracker, RedisPresenceTracker>();
        }
        else
        {
            services.AddSingleton<IPresenceTracker, InMemoryPresenceTracker>();
        }

        services.AddApplication();

        return services;
    }

    /// <summary>Connects to Redis if configured and reachable; returns null to trigger the in-memory fallback.</summary>
    public static IConnectionMultiplexer? TryConnectRedis(IConfiguration config)
    {
        var conn = config.GetConnectionString("Redis") ?? config["Redis"];
        if (string.IsNullOrWhiteSpace(conn))
            return null;
        try
        {
            var options = ConfigurationOptions.Parse(conn);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 2000;
            var mux = ConnectionMultiplexer.Connect(options);
            return mux.IsConnected ? mux : null;
        }
        catch
        {
            return null;
        }
    }
}
