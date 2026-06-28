using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Interfaces;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Application.Services;

public sealed class ReportService : IReportService
{
    private readonly IReceiptRepository _receipts;

    public ReportService(IReceiptRepository receipts)
    {
        _receipts = receipts;
    }

    public async Task<ReportSummaryResponse> GetMonthlySummaryAsync(
        int? userId, int? year, int? month,
        CancellationToken cancellationToken = default)
    {
        var rows = await _receipts.GetMonthlySummaryAsync(userId, year, month, cancellationToken)
            .ConfigureAwait(false);

        var totalCount = rows.Sum(r => r.Count);
        var totalAmount = rows.Sum(r => r.TotalAmount);
        var averageAmount = totalCount > 0 ? Math.Round(totalAmount / totalCount, 2) : 0m;

        var rowDtos = rows
            .Select(r => new MonthlyReportRow(r.Year, r.Month, r.Count, r.TotalAmount, r.AverageAmount))
            .ToList();

        return new ReportSummaryResponse(rowDtos, totalCount, totalAmount, averageAmount);
    }
}
