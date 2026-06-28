using System.ComponentModel.DataAnnotations;
using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Application.DTOs;

public sealed record CreateUserRequest(
    [Required, MaxLength(100)] string Username,
    [Required, MinLength(6), MaxLength(100)] string Password,
    [Required] string Role = UserRole.Driver,
    [MaxLength(200)] string? FullName = null);

public sealed record UserResponse(
    int Id,
    string Username,
    string FullName,
    string Role,
    bool IsActive);

public enum CreateUserStatus
{
    Created,
    UsernameAlreadyExists,
    InvalidRole
}

public sealed record CreateUserResult(
    CreateUserStatus Status,
    UserResponse? User = null);
