using ReceiptGenerator.Application.DTOs;

namespace ReceiptGenerator.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserResponse?> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> ActivateAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
