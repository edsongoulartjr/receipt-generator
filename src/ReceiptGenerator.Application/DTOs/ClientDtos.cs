using System.ComponentModel.DataAnnotations;

namespace ReceiptGenerator.Application.DTOs;

public sealed record ClientRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(500)] string Address = "",
    [MaxLength(50)] string TaxId = "",
    [MaxLength(9)] string? ZipCode = null,
    [MaxLength(300)] string? Street = null,
    [MaxLength(20)] string? Number = null,
    [MaxLength(100)] string? Complement = null,
    [MaxLength(100)] string? Neighborhood = null,
    [MaxLength(100)] string? City = null,
    [MaxLength(2)] string? State = null);

public sealed record ClientResponse(
    int Id,
    string Name,
    string Address,
    string TaxId,
    string? ZipCode,
    string? Street,
    string? Number,
    string? Complement,
    string? Neighborhood,
    string? City,
    string? State);
