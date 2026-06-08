using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReceiptGenerator.Application.Abstractions;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Infrastructure.Persistence;

namespace ReceiptGenerator.Infrastructure.Bootstrap;

public sealed class SuperAdminBootstrapHostedService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SuperAdminBootstrapHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public SuperAdminBootstrapHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<SuperAdminBootstrapHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var bootstrapEnabled = bool.TryParse(_configuration["BootstrapAdmin:Enabled"], out var enabled) && enabled;
        if (!bootstrapEnabled)
        {
            return;
        }

        var username = _configuration["BootstrapAdmin:Username"];
        var password = _configuration["BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("BootstrapAdmin is enabled, but username or password was not configured.");
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await dbContext.Users.AnyAsync(user => user.Role == UserRole.SuperAdmin, cancellationToken))
        {
            _logger.LogInformation("SuperAdmin bootstrap skipped because a SuperAdmin user already exists.");
            return;
        }

        username = username.Trim();
        var existingUser = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Username == username, cancellationToken);

        if (existingUser is not null)
        {
            existingUser.ChangeRole(UserRole.SuperAdmin);
            existingUser.Activate();
            _logger.LogInformation("Existing user was promoted to SuperAdmin by bootstrap.");
        }
        else
        {
            var superAdmin = new User(username, passwordHasher.Hash(password), UserRole.SuperAdmin);
            dbContext.Users.Add(superAdmin);
            _logger.LogInformation("Initial SuperAdmin user was created by bootstrap.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
