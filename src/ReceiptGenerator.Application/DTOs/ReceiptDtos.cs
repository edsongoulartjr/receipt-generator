using System.ComponentModel.DataAnnotations;

namespace ReceiptGenerator.Application.DTOs;

public sealed record ReceiptRequest(
    [Required] int ClientId,
    [Required, MaxLength(1000)] string Description,
    [Range(0.01, double.MaxValue)] decimal Amount,
    DateTime? StartTime,
    DateTime? EndTime,
    [MaxLength(100)] string? ServiceDates,
    [MaxLength(200)] string? IssuerName,
    [MaxLength(50)] string? IssuerPhone,
    [MaxLength(200)] string? IssuerEmail,
    int? DriverUserId = null);  // Apenas admin: emitir em nome de outro motorista

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ReceiptResponse(
    int Id,
    int Number,
    DateTime Date,
    string Description,
    decimal Amount,
    DateTime? StartTime,
    DateTime? EndTime,
    string? ServiceDates,
    string? IssuerName,
    string? IssuerPhone,
    string? IssuerEmail,
    string? DriverName,
    ClientResponse Client);
