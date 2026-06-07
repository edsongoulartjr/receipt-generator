using ReceiptGenerator.Application.DTOs;

namespace ReceiptGenerator.Application.Interfaces;

public interface IClientService
{
    Task<IReadOnlyList<ClientResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<ClientResponse?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<ClientResponse?> CreateAsync(int userId, ClientRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, int userId, ClientRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);
}
