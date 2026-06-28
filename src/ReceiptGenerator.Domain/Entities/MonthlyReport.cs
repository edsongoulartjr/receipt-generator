namespace ReceiptGenerator.Domain.Entities;

public sealed record MonthlyReport(int Year, int Month, int Count, decimal TotalAmount)
{
    public decimal AverageAmount => Count > 0 ? Math.Round(TotalAmount / Count, 2) : 0m;
}
