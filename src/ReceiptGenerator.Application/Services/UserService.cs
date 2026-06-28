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

    public async Task<IReadOnlyList<UserResponse>> GetActiveDriversAsync(CancellationToken cancellationToken = default)
    {
        var drivers = await _users.GetActiveDriversAsync(cancellationToken);
        return drivers.Select(Map).ToList();
    }

    public async Task<CreateUserResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!UserRole.IsValid(request.Role))
        {
            return new CreateUserResult(CreateUserStatus.InvalidRole);
        }

        if (await _users.GetByUsernameAsync(request.Username, cancellationToken) is not null)
        {
            return new CreateUserResult(CreateUserStatus.UsernameAlreadyExists);
        }

        var user = new User(request.Username, _passwordHasher.Hash(request.Password), request.Role, request.FullName);
        await _users.AddAsync(user, cancellationToken);
        return new CreateUserResult(CreateUserStatus.Created, Map(user));
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

    public async Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task<UpdateProfileResult> UpdateProfileAsync(
        int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return new UpdateProfileResult(UpdateProfileStatus.UserNotFound);
        }

        if (request.NewPassword is not null)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword)
                || !_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return new UpdateProfileResult(UpdateProfileStatus.WrongPassword);
            }

            user.ChangePasswordHash(_passwordHasher.Hash(request.NewPassword));
        }

        if (request.FullName is not null)
        {
            user.SetFullName(request.FullName);
        }

        await _users.UpdateAsync(user, cancellationToken);
        return new UpdateProfileResult(UpdateProfileStatus.Ok);
    }

    private static UserResponse Map(User user) =>
        new(user.Id, user.Username, user.FullName, user.Role, user.IsActive);
}
