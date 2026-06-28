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
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_users, _hasher, _tokenGenerator);
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

    [Fact(DisplayName = "Login returns a token response when credentials are valid")]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenInResponse()
    {
        var user = CreateUser(isActive: true);
        _users.GetByUsernameAsync(user.Username, Arg.Any<CancellationToken>())
            .Returns(user);
        _hasher.Verify("correct", user.PasswordHash).Returns(true);
        _tokenGenerator.Generate(user).Returns("jwt-token");

        var result = await _sut.LoginAsync(new LoginRequest(user.Username, "correct"));

        result.Should().NotBeNull();
        result!.Token.Should().Be("jwt-token");
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

    private static User CreateUser(bool isActive)
    {
        var user = new User("taxista01", "hashed-password", UserRole.Operator);
        if (!isActive) user.Deactivate();
        return user;
    }
}
