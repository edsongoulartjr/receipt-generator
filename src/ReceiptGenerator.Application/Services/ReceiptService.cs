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

    public async Task<IReadOnlyList<ReceiptResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var receipts = await _receipts.GetByUserIdAsync(userId, cancellationToken);
        return receipts.Select(Map).ToList();
    }

    public async Task<ReceiptResponse?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAndUserIdAsync(id, userId, cancellationToken);
        return receipt is null ? null : Map(receipt);
    }

    public async Task<ReceiptResponse?> CreateAsync(int userId, ReceiptRequest request, CancellationToken cancellationToken = default)
    {
        if (await _clients.GetByIdAndUserIdAsync(request.ClientId, userId, cancellationToken) is null)
        {
            return null;
        }

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

        await _receipts.AddAsync(receipt, cancellationToken);
        receipt = await _receipts.GetByIdAndUserIdAsync(receipt.Id, userId, cancellationToken) ?? receipt;
        return Map(receipt);
    }

    public async Task<bool> UpdateAsync(int id, int userId, ReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAndUserIdAsync(id, userId, cancellationToken);
        if (receipt is null || await _clients.GetByIdAndUserIdAsync(request.ClientId, userId, cancellationToken) is null)
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

        await _receipts.UpdateAsync(receipt, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAndUserIdAsync(id, userId, cancellationToken);
        if (receipt is null)
        {
            return false;
        }

        await _receipts.DeleteAsync(receipt, cancellationToken);
        return true;
    }

    public async Task<byte[]?> GeneratePdfAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetByIdAndUserIdAsync(id, userId, cancellationToken);
        return receipt is null ? null : _pdfGenerator.Generate(receipt);
    }

    private static ReceiptResponse Map(Receipt receipt)
    {
        var client = receipt.Client is null
            ? new ClientResponse(receipt.ClientId, string.Empty, string.Empty, string.Empty)
            : new ClientResponse(receipt.Client.Id, receipt.Client.Name, receipt.Client.Address, receipt.Client.TaxId);

        return new ReceiptResponse(
            receipt.Id,
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
