using Microsoft.EntityFrameworkCore;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Infrastructure.Persistence.Repositories;

public sealed class ClientRepository : IClientRepository
{
    private readonly ApplicationDbContext _context;

    public ClientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Client>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Client?> GetByIdAndUserIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        return _context.Clients.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Add(client);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Remove(client);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
