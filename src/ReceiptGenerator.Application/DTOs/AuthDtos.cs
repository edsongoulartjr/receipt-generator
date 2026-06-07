using System.ComponentModel.DataAnnotations;

namespace ReceiptGenerator.Application.DTOs;

public sealed record RegisterUserRequest(
    [Required, MaxLength(100)] string Username,
    [Required, MinLength(6), MaxLength(100)] string Password);

public sealed record LoginRequest(
    [Required, MaxLength(100)] string Username,
    [Required, MaxLength(100)] string Password);

public sealed record AuthResponse(string Token);
