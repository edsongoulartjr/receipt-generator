using ReceiptGenerator.Application.Abstractions;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Interfaces;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository users, IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users.GetAllAsync(cancellationToken);
        return users.Select(Map).ToList();
    }

    public async Task<UserResponse?> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!UserRole.IsValid(request.Role) || await _users.GetByUsernameAsync(request.Username, cancellationToken) is not null)
        {
            return null;
        }

        var user = new User(request.Username, _passwordHasher.Hash(request.Password), request.Role);
        await _users.AddAsync(user, cancellationToken);
        return Map(user);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.Activate();
        await _users.UpdateAsync(user, cancellationToken);
        return true;
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.Deactivate();
        await _users.UpdateAsync(user, cancellationToken);
        return true;
    }

    private static UserResponse Map(User user)
    {
        return new UserResponse(user.Id, user.Username, user.Role, user.IsActive);
    }
}
