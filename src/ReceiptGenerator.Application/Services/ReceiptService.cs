using ReceiptGenerator.Application.Abstractions;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Interfaces;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Application.Services;

public sealed class ReceiptService : IReceiptService
{
    private readonly IReceiptRepository _receipts;
    private readonly IClientRepository _clients;
    private readonly IUserRepository _users;
    private readonly IReceiptPdfGenerator _pdfGenerator;

    public ReceiptService(
        IReceiptRepository receipts,
        IClientRepository clients,
        IUserRepository users,
        IReceiptPdfGenerator pdfGenerator)
    {
        _receipts = receipts;
        _clients = clients;
        _users = users;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<PagedResponse<ReceiptResponse>> GetByUserIdAsync(
        int userId, int page, int pageSize, int? month = null, int? year = null,
        bool includeCancelled = false, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _receipts.GetByUserIdAsync(userId, page, pageSize, month, year, includeCancelled, cancellationToken).ConfigureAwait(false);
        return ToPagedResponse(items, page, pageSize, total);
    }

    public async Task<PagedResponse<ReceiptResponse>> GetAllAsync(
        int page, int pageSize, int? month = null, int? year = null,
        bool includeCancelled = false, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _receipts.GetAllPagedAsync(page, pageSize, month, year, includeCancelled, cancellationToken).ConfigureAwait(false);
        return ToPagedResponse(items, page, pageSize, total);
    }

    public async Task<ReceiptResponse?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAndUserIdAsync(id, userId, cancellationToken).ConfigureAwait(false);
        return receipt is null ? null : Map(receipt);
    }

    public async Task<ReceiptResponse?> GetByAnyIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return receipt is null ? null : Map(receipt);
    }

    public async Task<ReceiptResponse?> CreateAsync(
        int requestingUserId,
        string requestingUserRole,
        ReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        // Resolve o motorista: admin pode emitir em nome de outro; driver emite para si
        int driverUserId = UserRole.IsAdmin(requestingUserRole) && request.DriverUserId.HasValue
            ? request.DriverUserId.Value
            : requestingUserId;

        var driver = await _users.GetByIdAsync(driverUserId, cancellationToken).ConfigureAwait(false);
        if (driver is null || !driver.IsActive)
        {
            return null;
        }

        if (await _clients.GetByIdAndUserIdAsync(request.ClientId, driverUserId, cancellationToken).ConfigureAwait(false) is null)
        {
            return null;
        }

        var nextNumber = await _receipts.GetNextNumberAsync(driverUserId, cancellationToken).ConfigureAwait(false);

        var receipt = new Receipt(
            request.ClientId,
            driverUserId,
            request.Description,
            request.Amount,
            NormalizeDateTime(request.StartTime),
            NormalizeDateTime(request.EndTime),
            request.ServiceDates,
            request.IssuerName,
            request.IssuerPhone,
            request.IssuerEmail,
            driver.FullName);  // snapshot imutável do nome do motorista

        receipt.SetNumber(nextNumber);
        await _receipts.AddAsync(receipt, cancellationToken).ConfigureAwait(false);
        receipt = await _receipts.GetByIdAndUserIdAsync(receipt.Id, driverUserId, cancellationToken).ConfigureAwait(false) ?? receipt;
        return Map(receipt);
    }

    public async Task<bool> UpdateAsync(int id, int userId, ReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAndUserIdAsync(id, userId, cancellationToken).ConfigureAwait(false);
        if (receipt is null || await _clients.GetByIdAndUserIdAsync(request.ClientId, userId, cancellationToken).ConfigureAwait(false) is null)
        {
            return false;
        }

        receipt.ChangeClient(request.ClientId);
        receipt.Update(
            request.Description,
            request.Amount,
            NormalizeDateTime(request.StartTime),
            NormalizeDateTime(request.EndTime),
            request.ServiceDates,
            request.IssuerName,
            request.IssuerPhone,
            request.IssuerEmail);

        await _receipts.UpdateAsync(receipt, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> UpdateByAnyIdAsync(int id, ReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (receipt is null || await _clients.GetByIdAndUserIdAsync(request.ClientId, receipt.UserId, cancellationToken).ConfigureAwait(false) is null)
        {
            return false;
        }

        receipt.ChangeClient(request.ClientId);
        receipt.Update(
            request.Description,
            request.Amount,
            NormalizeDateTime(request.StartTime),
            NormalizeDateTime(request.EndTime),
            request.ServiceDates,
            request.IssuerName,
            request.IssuerPhone,
            request.IssuerEmail);

        await _receipts.UpdateAsync(receipt, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAndUserIdAsync(id, userId, cancellationToken).ConfigureAwait(false);
        if (receipt is null || receipt.IsCancelled)
        {
            return false;
        }

        await _receipts.CancelAsync(receipt, reason, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> DeleteByAnyIdAsync(int id, string? reason = null, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (receipt is null || receipt.IsCancelled)
        {
            return false;
        }

        await _receipts.CancelAsync(receipt, reason, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<byte[]?> GeneratePdfAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAndUserIdAsync(id, userId, cancellationToken).ConfigureAwait(false);
        return receipt is null ? null : _pdfGenerator.Generate(receipt);
    }

    public async Task<byte[]?> GeneratePdfByAnyIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return receipt is null ? null : _pdfGenerator.Generate(receipt);
    }

    private static PagedResponse<ReceiptResponse> ToPagedResponse(
        IReadOnlyList<Receipt> items, int page, int pageSize, int total)
    {
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling((double)total / pageSize);
        return new PagedResponse<ReceiptResponse>(items.Select(Map).ToList(), page, pageSize, total, totalPages);
    }

    private static ReceiptResponse Map(Receipt receipt)
    {
        var client = receipt.Client is null
            ? new ClientResponse(receipt.ClientId, string.Empty, string.Empty, string.Empty)
            : new ClientResponse(receipt.Client.Id, receipt.Client.Name, receipt.Client.Address, receipt.Client.TaxId);

        return new ReceiptResponse(
            receipt.Id,
            receipt.Number,
            receipt.Date,
            receipt.Description,
            receipt.Amount,
            receipt.StartTime,
            receipt.EndTime,
            receipt.ServiceDates,
            receipt.IssuerName,
            receipt.IssuerPhone,
            receipt.IssuerEmail,
            receipt.DriverName,
            client,
            receipt.CancelledAt,
            receipt.CancelReason);
    }

    private static DateTime? NormalizeDateTime(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }
}
