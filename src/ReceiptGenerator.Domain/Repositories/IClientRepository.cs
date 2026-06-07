using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Domain.Repositories;

public interface IClientRepository
{
    Task<IReadOnlyList<Client>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Client?> GetByIdAndUserIdAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task AddAsync(Client client, CancellationToken cancellationToken = default);
    Task UpdateAsync(Client client, CancellationToken cancellationToken = default);
    Task DeleteAsync(Client client, CancellationToken cancellationToken = default);
}
