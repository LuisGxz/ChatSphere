using ChatSphere.Application.Auth;
using ChatSphere.Application.Chat;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ChatSphere.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IChatWriteService, ChatWriteService>();
        return services;
    }
}
