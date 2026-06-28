using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReceiptGenerator.Application.Abstractions;
using ReceiptGenerator.Domain.Repositories;
using ReceiptGenerator.Infrastructure.Bootstrap;
using ReceiptGenerator.Infrastructure.Pdf;
using ReceiptGenerator.Infrastructure.Persistence;
using ReceiptGenerator.Infrastructure.Persistence.Repositories;
using ReceiptGenerator.Infrastructure.Security;

namespace ReceiptGenerator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CooperativeSettings>(options =>
        {
            var section = configuration.GetSection(CooperativeSettings.SectionName);
            options.Name = section["Name"] ?? string.Empty;
            options.LegalName = section["LegalName"] ?? string.Empty;
            options.TaxId = section["TaxId"] ?? string.Empty;
            options.Phone = section["Phone"] ?? string.Empty;
            options.Email = section["Email"] ?? string.Empty;
            options.City = section["City"] ?? string.Empty;
        });

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IReceiptRepository, ReceiptRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IReceiptPdfGenerator, QuestReceiptPdfGenerator>();
        services.AddHostedService<SuperAdminBootstrapHostedService>();

        return services;
    }
}
