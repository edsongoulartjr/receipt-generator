using ReceiptGenerator.Application.Abstractions;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Interfaces;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    public AuthService(IUserRepository users, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<bool> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        if (await _users.GetByUsernameAsync(request.Username, cancellationToken) is not null)
        {
            return false;
        }

        var user = new User(request.Username, _passwordHasher.Hash(request.Password));
        await _users.AddAsync(user, cancellationToken);
        return true;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        return new AuthResponse(_tokenGenerator.Generate(user));
    }
}
