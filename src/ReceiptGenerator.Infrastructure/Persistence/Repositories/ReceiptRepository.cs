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

    public async Task<(IReadOnlyList<Receipt> Items, int TotalCount)> GetByUserIdAsync(
        int userId, int page, int pageSize, int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery()
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        query = ApplyPeriodFilter(query, month, year);
        query = query.OrderByDescending(x => x.Date);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Receipt> Items, int TotalCount)> GetAllPagedAsync(
        int page, int pageSize, int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery().AsNoTracking();

        query = ApplyPeriodFilter(query, month, year);
        query = query.OrderByDescending(x => x.Date);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, total);
    }

    private static IQueryable<Receipt> ApplyPeriodFilter(IQueryable<Receipt> query, int? month, int? year)
    {
        if (year.HasValue) query = query.Where(x => x.Date.Year == year.Value);
        if (month.HasValue) query = query.Where(x => x.Date.Month == month.Value);
        return query;
    }

    public Task<Receipt?> GetByIdAndUserIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        return BaseQuery().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
    }

    public Task<Receipt?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return BaseQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MonthlyReport>> GetMonthlySummaryAsync(
        int? userId, int? year, int? month, CancellationToken cancellationToken = default)
    {
        var query = _context.Receipts.AsQueryable();
        if (userId.HasValue) query = query.Where(x => x.UserId == userId.Value);
        if (year.HasValue) query = query.Where(x => x.Date.Year == year.Value);
        if (month.HasValue) query = query.Where(x => x.Date.Month == month.Value);
        return await QueryMonthlySummary(query, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<MonthlyReport>> QueryMonthlySummary(
        IQueryable<Receipt> source, CancellationToken cancellationToken)
    {
        // EF Core não traduz construtores posicionais de record diretamente no Select após GroupBy.
        // Projeta para tipo anônimo no SQL (suportado) e materializa em memória.
        var rows = await source
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(x => new MonthlyReport(x.Year, x.Month, x.Count, x.TotalAmount)).ToList();
    }

    public async Task<int> GetNextNumberAsync(int userId, CancellationToken cancellationToken = default)
    {
        var max = await _context.Receipts
            .Where(x => x.UserId == userId)
            .MaxAsync(x => (int?)x.Number, cancellationToken)
            .ConfigureAwait(false);

        return (max ?? 0) + 1;
    }

    public async Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        _context.Receipts.Remove(receipt);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<Receipt> BaseQuery()
    {
        return _context.Receipts
            .Include(x => x.Client)
            .Include(x => x.User);
    }
}
