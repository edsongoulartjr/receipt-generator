using AwesomeAssertions;
using NSubstitute;
using ReceiptGenerator.Application.Services;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Tests.Application;

public sealed class ReportServiceTests
{
    private readonly IReceiptRepository _receipts = Substitute.For<IReceiptRepository>();
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _sut = new ReportService(_receipts);
    }

    [Fact(DisplayName = "GetMonthlySummary returns mapped rows with computed totals and average")]
    public async Task GetMonthlySummaryAsync_ReturnsMappedSummaryWithTotals()
    {
        var reports = new List<MonthlyReport>
        {
            new(2026, 6, 5, 750m),
            new(2026, 5, 3, 420m)
        };
        _receipts.GetMonthlySummaryAsync(10, null, null, Arg.Any<CancellationToken>())
            .Returns(reports);

        var result = await _sut.GetMonthlySummaryAsync(10, null, null);

        result.Rows.Should().HaveCount(2);
        result.Rows[0].Year.Should().Be(2026);
        result.Rows[0].Month.Should().Be(6);
        result.Rows[0].Count.Should().Be(5);
        result.Rows[0].TotalAmount.Should().Be(750m);
        result.Rows[0].AverageAmount.Should().Be(150m);
        result.TotalCount.Should().Be(8);
        result.TotalAmount.Should().Be(1170m);
        result.AverageAmount.Should().Be(146.25m);
    }

    [Fact(DisplayName = "GetMonthlySummary returns empty rows when user has no receipts")]
    public async Task GetMonthlySummaryAsync_WhenNoReceipts_ReturnsEmptySummary()
    {
        _receipts.GetMonthlySummaryAsync(99, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<MonthlyReport>());

        var result = await _sut.GetMonthlySummaryAsync(99, null, null);

        result.Rows.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalAmount.Should().Be(0m);
        result.AverageAmount.Should().Be(0m);
    }

    [Fact(DisplayName = "GetMonthlySummary passes year and month filters to repository")]
    public async Task GetMonthlySummaryAsync_WithFilters_PassesFiltersToRepository()
    {
        _receipts.GetMonthlySummaryAsync(null, 2026, 6, Arg.Any<CancellationToken>())
            .Returns(new List<MonthlyReport>());

        await _sut.GetMonthlySummaryAsync(null, 2026, 6);

        await _receipts.Received(1).GetMonthlySummaryAsync(null, 2026, 6, Arg.Any<CancellationToken>());
    }
}
