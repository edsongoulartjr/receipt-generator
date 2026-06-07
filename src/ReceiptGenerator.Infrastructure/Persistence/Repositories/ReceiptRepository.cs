using Microsoft.EntityFrameworkCore;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Infrastructure.Persistence.Repositories;

public sealed class ReceiptRepository : IReceiptRepository
{
    private readonly ApplicationDbContext _context;

    public ReceiptRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Receipt>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await BaseQuery()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<Receipt?> GetByIdAndUserIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        return BaseQuery().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        _context.Receipts.Remove(receipt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Receipt> BaseQuery()
    {
        return _context.Receipts
            .Include(x => x.Client)
            .Include(x => x.User);
    }
}
