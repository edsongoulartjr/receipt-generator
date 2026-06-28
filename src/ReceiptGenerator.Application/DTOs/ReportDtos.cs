namespace ReceiptGenerator.Application.DTOs;

public sealed record MonthlyReportRow(int Year, int Month, int Count, decimal TotalAmount, decimal AverageAmount);

public sealed record ReportSummaryResponse(
    IReadOnlyList<MonthlyReportRow> Rows,
    int TotalCount,
    decimal TotalAmount,
    decimal AverageAmount);
