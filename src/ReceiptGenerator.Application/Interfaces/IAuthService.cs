using ReceiptGenerator.Application.DTOs;

namespace ReceiptGenerator.Application.Interfaces;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
