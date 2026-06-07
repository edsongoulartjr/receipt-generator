using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Domain.Repositories;

public interface IReceiptRepository
{
    Task<IReadOnlyList<Receipt>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Receipt?> GetByIdAndUserIdAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default);
    Task UpdateAsync(Receipt receipt, CancellationToken cancellationToken = default);
    Task DeleteAsync(Receipt receipt, CancellationToken cancellationToken = default);
}
