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
    private readonly IReceiptPdfGenerator _pdfGenerator;

    public ReceiptService(IReceiptRepository receipts, IClientRepository clients, IReceiptPdfGenerator pdfGenerator)
    {
        _receipts = receipts;
        _clients = clients;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<PagedResponse<ReceiptResponse>> GetByUserIdAsync(
        int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _receipts.GetByUserIdAsync(userId, page, pageSize, cancellationToken).ConfigureAwait(false);
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling((double)total / pageSize);
        return new PagedResponse<ReceiptResponse>(items.Select(Map).ToList(), page, pageSize, total, totalPages);
    }

    public async Task<IReadOnlyList<MonthlyReportResponse>> GetMonthlySummaryAsync(int userId, CancellationToken cancellationToken = default)
    {
        var summary = await _receipts.GetMonthlySummaryAsync(userId, cancellationToken).ConfigureAwait(false);
        return summary.Select(x => new MonthlyReportResponse(x.Year, x.Month, x.Count, x.TotalAmount)).ToList();
    }

    public async Task<ReceiptResponse?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAndUserIdAsync(id, userId, cancellationToken).ConfigureAwait(false);
        return receipt is null ? null : Map(receipt);
    }

    public async Task<ReceiptResponse?> CreateAsync(int userId, ReceiptRequest request, CancellationToken cancellationToken = default)
    {
        if (await _clients.GetByIdAndUserIdAsync(request.ClientId, userId, cancellationToken).ConfigureAwait(false) is null)
        {
            return null;
        }

        var nextNumber = await _receipts.GetNextNumberAsync(userId, cancellationToken).ConfigureAwait(false);

        var receipt = new Receipt(
            request.ClientId,
            userId,
            request.Description,
            request.Amount,
            NormalizeDateTime(request.StartTime),
            NormalizeDateTime(request.EndTime),
            request.ServiceDates,
            request.IssuerName,
            request.IssuerPhone,
            request.IssuerEmail,
            request.DriverName);

        receipt.SetNumber(nextNumber);
        await _receipts.AddAsync(receipt, cancellationToken).ConfigureAwait(false);
        receipt = await _receipts.GetByIdAndUserIdAsync(receipt.Id, userId, cancellationToken).ConfigureAwait(false) ?? receipt;
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
            request.IssuerEmail,
            request.DriverName);

        await _receipts.UpdateAsync(receipt, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAndUserIdAsync(id, userId, cancellationToken).ConfigureAwait(false);
        if (receipt is null)
        {
            return false;
        }

        await _receipts.DeleteAsync(receipt, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<byte[]?> GeneratePdfAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAndUserIdAsync(id, userId, cancellationToken).ConfigureAwait(false);
        return receipt is null ? null : _pdfGenerator.Generate(receipt);
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
            client);
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
