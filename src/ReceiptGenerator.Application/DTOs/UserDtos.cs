using System.ComponentModel.DataAnnotations;
using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Application.DTOs;

public sealed record CreateUserRequest(
    [Required, MaxLength(100)] string Username,
    [Required, MinLength(6), MaxLength(100)] string Password,
    [Required] string Role = UserRole.Operator);

public sealed record UserResponse(
    int Id,
    string Username,
    string Role,
    bool IsActive);
