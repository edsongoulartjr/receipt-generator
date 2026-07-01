using ReceiptGenerator.Application.Abstractions;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Interfaces;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Application.Services;

public sealed class AuthService : IAuthService
{
    private const int RefreshTokenExpiryDays = 180;
    private const int AccessTokenExpirySeconds = 3600; // 60 min — deve coincidir com JwtSettings:AccessTokenExpiryMinutes

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByUsernameAsync(request.Username, cancellationToken).ConfigureAwait(false);
        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        return await IssueTokenPairAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AuthResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenGenerator.Hash(request.RefreshToken);
        var user = await _users.GetByRefreshTokenHashAsync(tokenHash, cancellationToken).ConfigureAwait(false);

        // Valida existência, status e validade do token (inclui verificação de expiração)
        if (user is null || !user.IsActive || !user.HasValidRefreshToken(tokenHash))
        {
            return null;
        }

        return await IssueTokenPairAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> LogoutAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return false;
        }

        user.ClearRefreshToken();
        await _users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task<AuthResponse> IssueTokenPairAsync(User user, CancellationToken cancellationToken)
    {
        var refreshToken = _refreshTokenGenerator.Generate();
        var refreshTokenHash = _refreshTokenGenerator.Hash(refreshToken);
        var expiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        user.SetRefreshToken(refreshTokenHash, expiry);
        await _users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        return new AuthResponse(
            _tokenGenerator.Generate(user),
            refreshToken,
            AccessTokenExpirySeconds);
    }
}
