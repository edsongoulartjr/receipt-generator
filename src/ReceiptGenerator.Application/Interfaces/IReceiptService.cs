using ReceiptGenerator.Application.DTOs;

namespace ReceiptGenerator.Application.Interfaces;

public interface IReceiptService
{
    Task<PagedResponse<ReceiptResponse>> GetByUserIdAsync(int userId, int page, int pageSize, int? month = null, int? year = null, CancellationToken cancellationToken = default);
    Task<PagedResponse<ReceiptResponse>> GetAllAsync(int page, int pageSize, int? month = null, int? year = null, CancellationToken cancellationToken = default);
Task<ReceiptResponse?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<ReceiptResponse?> GetByAnyIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ReceiptResponse?> CreateAsync(int requestingUserId, string requestingUserRole, ReceiptRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, int userId, ReceiptRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateByAnyIdAsync(int id, ReceiptRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteByAnyIdAsync(int id, CancellationToken cancellationToken = default);
    Task<byte[]?> GeneratePdfAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<byte[]?> GeneratePdfByAnyIdAsync(int id, CancellationToken cancellationToken = default);
}
