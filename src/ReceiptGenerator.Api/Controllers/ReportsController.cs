using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Interfaces;
using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("monthly-summary")]
    public async Task<ActionResult<ReportSummaryResponse>> GetMonthlySummary(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] int? driverId,
        CancellationToken cancellationToken)
    {
        // Drivers always see only their own data; driverId param is admin-only
        int? scopedUserId = IsAdmin ? driverId : UserId;

        var result = await _reportService.GetMonthlySummaryAsync(scopedUserId, year, month, cancellationToken);
        return Ok(result);
    }

    private bool IsAdmin =>
        User.IsInRole(UserRole.SystemAdmin) || User.IsInRole(UserRole.CoopAdmin);

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User id claim was not found."));
}
