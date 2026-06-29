using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Domain.Repositories;

public interface IReceiptRepository
{
    Task<(IReadOnlyList<Receipt> Items, int TotalCount)> GetByUserIdAsync(int userId, int page, int pageSize, int? month = null, int? year = null, bool includeCancelled = false, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Receipt> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize, int? month = null, int? year = null, bool includeCancelled = false, CancellationToken cancellationToken = default);
    Task<Receipt?> GetByIdAndUserIdAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<Receipt?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> GetNextNumberAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyReport>> GetMonthlySummaryAsync(int? userId, int? year, int? month, CancellationToken cancellationToken = default);
    Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default);
    Task UpdateAsync(Receipt receipt, CancellationToken cancellationToken = default);
    Task CancelAsync(Receipt receipt, string? reason, CancellationToken cancellationToken = default);
}
