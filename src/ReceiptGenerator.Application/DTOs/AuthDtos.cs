using System.ComponentModel.DataAnnotations;

namespace ReceiptGenerator.Application.DTOs;

public sealed record LoginRequest(
    [Required, MaxLength(100)] string Username,
    [Required, MaxLength(100)] string Password);

public sealed record RefreshRequest(
    [Required] string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);
