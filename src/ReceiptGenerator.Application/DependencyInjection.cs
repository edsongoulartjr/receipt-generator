using Microsoft.Extensions.DependencyInjection;
using ReceiptGenerator.Application.Interfaces;
using ReceiptGenerator.Application.Services;

namespace ReceiptGenerator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IReceiptService, ReceiptService>();
        return services;
    }
}
