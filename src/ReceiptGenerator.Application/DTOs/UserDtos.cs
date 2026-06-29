using System.ComponentModel.DataAnnotations;
using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Application.DTOs;

public sealed record CreateUserRequest(
    [Required, MaxLength(100)] string Username,
    [Required, MinLength(6), MaxLength(100)] string Password,
    [Required] string Role = UserRole.Driver,
    [MaxLength(200)] string? FullName = null,
    [MaxLength(50)] string? Phone = null,
    [MaxLength(200)] string? Email = null);

public sealed record UserResponse(
    int Id,
    string Username,
    string FullName,
    string Role,
    bool IsActive,
    string? Phone,
    string? Email,
    DateTime? UpdatedAt);

public enum CreateUserStatus
{
    Created,
    UsernameAlreadyExists,
    InvalidRole
}

public sealed record CreateUserResult(
    CreateUserStatus Status,
    UserResponse? User = null);

public sealed record UpdateProfileRequest(
    [MaxLength(200)] string? FullName,
    string? CurrentPassword,
    [MinLength(6), MaxLength(100)] string? NewPassword,
    [MaxLength(50)] string? Phone,
    [MaxLength(200)] string? Email);

public enum UpdateProfileStatus
{
    Ok,
    UserNotFound,
    WrongPassword,
    NewPasswordRequired
}

public sealed record UpdateProfileResult(UpdateProfileStatus Status);

public sealed record ResetPasswordRequest(
    [Required, MinLength(6), MaxLength(100)] string NewPassword);
