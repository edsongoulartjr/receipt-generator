using AwesomeAssertions;
using NSubstitute;
using ReceiptGenerator.Application.Abstractions;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Services;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Tests.Application;

public sealed class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly IRefreshTokenGenerator _refreshTokenGenerator = Substitute.For<IRefreshTokenGenerator>();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _refreshTokenGenerator.Generate().Returns("opaque-refresh-token");
        _refreshTokenGenerator.Hash(Arg.Any<string>()).Returns("hashed-refresh-token");
        _sut = new AuthService(_users, _hasher, _tokenGenerator, _refreshTokenGenerator);
    }

    [Fact(DisplayName = "Login returns null when the username does not exist")]
    public async Task LoginAsync_WhenUserNotFound_ReturnsNull()
    {
        _users.GetByUsernameAsync("unknown", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.LoginAsync(new LoginRequest("unknown", "pass"));

        result.Should().BeNull();
    }

    [Fact(DisplayName = "Login returns null when the user account is inactive")]
    public async Task LoginAsync_WhenUserIsInactive_ReturnsNull()
    {
        var user = CreateUser(isActive: false);
        _users.GetByUsernameAsync(user.Username, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.LoginAsync(new LoginRequest(user.Username, "qualquer-senha"));

        result.Should().BeNull();
    }

    [Fact(DisplayName = "Login returns null when the password does not match")]
    public async Task LoginAsync_WhenPasswordIsWrong_ReturnsNull()
    {
        var user = CreateUser(isActive: true);
        _users.GetByUsernameAsync(user.Username, Arg.Any<CancellationToken>())
            .Returns(user);
        _hasher.Verify("wrong", user.PasswordHash).Returns(false);

        var result = await _sut.LoginAsync(new LoginRequest(user.Username, "wrong"));

        result.Should().BeNull();
    }

    [Fact(DisplayName = "Login returns access token when credentials are valid")]
    public async Task LoginAsync_WithValidCredentials_ReturnsAccessTokenInResponse()
    {
        var user = CreateUser(isActive: true);
        _users.GetByUsernameAsync(user.Username, Arg.Any<CancellationToken>())
            .Returns(user);
        _hasher.Verify("correct", user.PasswordHash).Returns(true);
        _tokenGenerator.Generate(user).Returns("jwt-token");

        var result = await _sut.LoginAsync(new LoginRequest(user.Username, "correct"));

        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("jwt-token");
        result.RefreshToken.Should().Be("opaque-refresh-token");
        result.ExpiresIn.Should().Be(3600);
    }

    [Fact(DisplayName = "Login calls the token generator exactly once when credentials are valid")]
    public async Task LoginAsync_WithValidCredentials_CallsTokenGeneratorOnce()
    {
        var user = CreateUser(isActive: true);
        _users.GetByUsernameAsync(user.Username, Arg.Any<CancellationToken>())
            .Returns(user);
        _hasher.Verify("correct", user.PasswordHash).Returns(true);
        _tokenGenerator.Generate(user).Returns("jwt-token");

        await _sut.LoginAsync(new LoginRequest(user.Username, "correct"));

        _tokenGenerator.Received(1).Generate(user);
    }

    [Fact(DisplayName = "Refresh returns null when refresh token is not found in DB")]
    public async Task RefreshAsync_WhenTokenNotFound_ReturnsNull()
    {
        _users.GetByRefreshTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.RefreshAsync(new RefreshRequest("invalid-token"));

        result.Should().BeNull();
    }

    [Fact(DisplayName = "Refresh returns null when user is inactive")]
    public async Task RefreshAsync_WhenUserIsInactive_ReturnsNull()
    {
        var user = CreateUser(isActive: false);
        _users.GetByRefreshTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.RefreshAsync(new RefreshRequest("some-token"));

        result.Should().BeNull();
    }

    [Fact(DisplayName = "Logout clears the refresh token and returns true")]
    public async Task LogoutAsync_WhenUserExists_ClearsRefreshTokenAndReturnsTrue()
    {
        var user = CreateUser(isActive: true);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.LogoutAsync(user.Id);

        result.Should().BeTrue();
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Logout returns false when user does not exist")]
    public async Task LogoutAsync_WhenUserNotFound_ReturnsFalse()
    {
        _users.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.LogoutAsync(99);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Refresh returns new tokens when token is valid and user is active")]
    public async Task RefreshAsync_WhenTokenIsValidAndUserIsActive_ReturnsNewTokens()
    {
        var user = CreateUser(isActive: true);
        // Simula token válido: o token bruto é "my-token", hash = "hashed-refresh-token"
        user.SetRefreshToken("hashed-refresh-token", DateTime.UtcNow.AddDays(1));
        _refreshTokenGenerator.Hash("my-token").Returns("hashed-refresh-token");
        _users.GetByRefreshTokenHashAsync("hashed-refresh-token", Arg.Any<CancellationToken>())
            .Returns(user);
        _tokenGenerator.Generate(user).Returns("new-jwt-token");

        var result = await _sut.RefreshAsync(new RefreshRequest("my-token"));

        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("new-jwt-token");
        result.RefreshToken.Should().Be("opaque-refresh-token");
    }

    [Fact(DisplayName = "Refresh returns null when the stored token is expired")]
    public async Task RefreshAsync_WhenTokenIsExpired_ReturnsNull()
    {
        var user = CreateUser(isActive: true);
        // Token expirado (no passado)
        user.SetRefreshToken("hashed-refresh-token", DateTime.UtcNow.AddSeconds(-1));
        _refreshTokenGenerator.Hash("my-token").Returns("hashed-refresh-token");
        _users.GetByRefreshTokenHashAsync("hashed-refresh-token", Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.RefreshAsync(new RefreshRequest("my-token"));

        result.Should().BeNull();
    }

    [Fact(DisplayName = "Logout clears refresh token on the user entity before persisting")]
    public async Task LogoutAsync_WhenUserExists_ClearsRefreshTokenOnEntity()
    {
        var user = CreateUser(isActive: true);
        user.SetRefreshToken("some-hash", DateTime.UtcNow.AddDays(30));
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await _sut.LogoutAsync(user.Id);

        user.RefreshTokenHash.Should().BeNull();
        user.RefreshTokenExpiry.Should().BeNull();
    }

    private static User CreateUser(bool isActive)
    {
        var user = new User("taxista01", "hashed-password", UserRole.Driver);
        if (!isActive) user.Deactivate();
        return user;
    }
}
