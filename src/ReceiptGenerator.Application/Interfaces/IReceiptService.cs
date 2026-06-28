using ReceiptGenerator.Application.DTOs;

namespace ReceiptGenerator.Application.Interfaces;

public interface IReceiptService
{
    Task<PagedResponse<ReceiptResponse>> GetByUserIdAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyReportResponse>> GetMonthlySummaryAsync(int userId, CancellationToken cancellationToken = default);
    Task<ReceiptResponse?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<ReceiptResponse?> CreateAsync(int userId, ReceiptRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, int userId, ReceiptRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<byte[]?> GeneratePdfAsync(int id, int userId, CancellationToken cancellationToken = default);
}
