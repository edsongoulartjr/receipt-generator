using System.ComponentModel.DataAnnotations;

namespace ReceiptGenerator.Application.DTOs;

public sealed record ClientRequest(
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(500)] string Address,
    [Required, MaxLength(50)] string TaxId);

public sealed record ClientResponse(int Id, string Name, string Address, string TaxId);
