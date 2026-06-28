namespace ReceiptGenerator.Domain.Entities;

public sealed record MonthlyReport(int Year, int Month, int Count, decimal TotalAmount);
