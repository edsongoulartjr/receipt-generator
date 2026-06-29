using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Interfaces;
using DomainRoles = ReceiptGenerator.Domain.Entities.UserRole;

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
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        [FromQuery] bool includeCancelled = false,
        CancellationToken cancellationToken = default)
    {
        // Somente admins podem ver recibos cancelados
        var showCancelled = IsAdmin && includeCancelled;

        if (IsAdmin)
            return Ok(await _receiptService.GetAllAsync(page, pageSize, month, year, showCancelled, cancellationToken));

        return Ok(await _receiptService.GetByUserIdAsync(UserId, page, pageSize, month, year, showCancelled, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReceiptResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var receipt = IsAdmin
            ? await _receiptService.GetByAnyIdAsync(id, cancellationToken)
            : await _receiptService.GetByIdAsync(id, UserId, cancellationToken);

        return receipt is null ? NotFound() : Ok(receipt);
    }

    [HttpPost]
    public async Task<ActionResult<ReceiptResponse>> Create(ReceiptRequest request, CancellationToken cancellationToken)
    {
        var receipt = await _receiptService.CreateAsync(UserId, UserRole, request, cancellationToken);
        return receipt is null
            ? BadRequest("Motorista ou cliente não encontrado, ou inativo.")
            : CreatedAtAction(nameof(GetById), new { id = receipt.Id }, receipt);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ReceiptRequest request, CancellationToken cancellationToken)
    {
        var updated = IsAdmin
            ? await _receiptService.UpdateByAnyIdAsync(id, request, cancellationToken)
            : await _receiptService.UpdateAsync(id, UserId, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        [FromBody] CancelReceiptRequest? request,
        CancellationToken cancellationToken)
    {
        var deleted = IsAdmin
            ? await _receiptService.DeleteByAnyIdAsync(id, request?.Reason, cancellationToken)
            : await _receiptService.DeleteAsync(id, UserId, request?.Reason, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id, CancellationToken cancellationToken)
    {
        var pdf = IsAdmin
            ? await _receiptService.GeneratePdfByAnyIdAsync(id, cancellationToken)
            : await _receiptService.GeneratePdfAsync(id, UserId, cancellationToken);

        return pdf is null
            ? NotFound()
            : File(pdf, "application/pdf", $"recibo-{id}.pdf");
    }

    private bool IsAdmin =>
        User.IsInRole(DomainRoles.SystemAdmin) || User.IsInRole(DomainRoles.CoopAdmin);

    private string UserRole =>
        User.FindFirstValue(ClaimTypes.Role)
        ?? throw new InvalidOperationException("Role claim was not found.");

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User id claim was not found."));
}
