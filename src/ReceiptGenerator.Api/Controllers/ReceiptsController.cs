using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Interfaces;

namespace ReceiptGenerator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/receipts")]
public sealed class ReceiptsController : ControllerBase
{
    private readonly IReceiptService _receiptService;

    public ReceiptsController(IReceiptService receiptService)
    {
        _receiptService = receiptService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ReceiptResponse>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _receiptService.GetByUserIdAsync(UserId, page, pageSize, cancellationToken));
    }

    [HttpGet("monthly-summary")]
    public async Task<ActionResult<IReadOnlyList<MonthlyReportResponse>>> GetMonthlySummary(CancellationToken cancellationToken)
    {
        return Ok(await _receiptService.GetMonthlySummaryAsync(UserId, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReceiptResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var receipt = await _receiptService.GetByIdAsync(id, UserId, cancellationToken);
        return receipt is null ? NotFound() : Ok(receipt);
    }

    [HttpPost]
    public async Task<ActionResult<ReceiptResponse>> Create(ReceiptRequest request, CancellationToken cancellationToken)
    {
        var receipt = await _receiptService.CreateAsync(UserId, request, cancellationToken);
        return receipt is null
            ? BadRequest("Client not found or does not belong to the authenticated user.")
            : CreatedAtAction(nameof(GetById), new { id = receipt.Id }, receipt);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ReceiptRequest request, CancellationToken cancellationToken)
    {
        var updated = await _receiptService.UpdateAsync(id, UserId, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _receiptService.DeleteAsync(id, UserId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id, CancellationToken cancellationToken)
    {
        var pdf = await _receiptService.GeneratePdfAsync(id, UserId, cancellationToken);
        return pdf is null
            ? NotFound()
            : File(pdf, "application/pdf", $"recibo-{id}.pdf");
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User id claim was not found."));
}
