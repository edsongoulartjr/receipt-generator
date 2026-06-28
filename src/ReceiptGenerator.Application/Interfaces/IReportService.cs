using ReceiptGenerator.Application.DTOs;

namespace ReceiptGenerator.Application.Interfaces;

public interface IReportService
{
    Task<ReportSummaryResponse> GetMonthlySummaryAsync(
        int? userId, int? year, int? month,
        CancellationToken cancellationToken = default);
}
