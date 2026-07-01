using AwesomeAssertions;
using NSubstitute;
using ReceiptGenerator.Application.Abstractions;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Services;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Tests.Application;

public sealed class UserServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_users, _hasher);
    }

    // -----------------------------------------------------------------------
    // GetAllAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "GetAll returns all users mapped to response DTOs")]
    public async Task GetAllAsync_ReturnsMappedUsers()
    {
        var list = new List<User>
        {
            new("taxista01", "hash", UserRole.Driver, "Carlos"),
            new("admin01", "hash", UserRole.CoopAdmin)
        };
        _users.GetAllAsync(Arg.Any<CancellationToken>()).Returns(list);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Username.Should().Be("taxista01");
        result[1].Username.Should().Be("admin01");
    }

    [Fact(DisplayName = "GetAll returns an empty list when no users exist")]
    public async Task GetAllAsync_WhenNoUsers_ReturnsEmptyList()
    {
        _users.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User>());

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // GetActiveDriversAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "GetActiveDrivers returns only active drivers")]
    public async Task GetActiveDriversAsync_ReturnsMappedActiveDrivers()
    {
        var drivers = new List<User> { new("motorista01", "hash", UserRole.Driver, "Ana") };
        _users.GetActiveDriversAsync(Arg.Any<CancellationToken>()).Returns(drivers);

        var result = await _sut.GetActiveDriversAsync();

        result.Should().HaveCount(1);
        result[0].Username.Should().Be("motorista01");
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "GetById returns null when user does not exist")]
    public async Task GetByIdAsync_WhenUserNotFound_ReturnsNull()
    {
        _users.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.GetByIdAsync(99);

        result.Should().BeNull();
    }

    [Fact(DisplayName = "GetById returns a mapped user response when found")]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedResponse()
    {
        var user = new User("taxista01", "hash", UserRole.Driver, "Carlos");
        _users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Username.Should().Be("taxista01");
        result.FullName.Should().Be("Carlos");
        result.Role.Should().Be(UserRole.Driver);
        result.IsActive.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Create returns Created status and persists user when data is valid")]
    public async Task CreateAsync_WithValidData_ReturnsCreatedAndPersistsUser()
    {
        _users.GetByUsernameAsync("taxista01", Arg.Any<CancellationToken>()).Returns((User?)null);
        _hasher.Hash("senha123").Returns("hashed-senha123");

        var request = new CreateUserRequest("taxista01", "senha123", UserRole.Driver, "Carlos");
        var result = await _sut.CreateAsync(request);

        result.Status.Should().Be(CreateUserStatus.Created);
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("taxista01");
        await _users.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Create returns UsernameAlreadyExists and does not persist when username is taken")]
    public async Task CreateAsync_WhenUsernameAlreadyExists_ReturnsUsernameAlreadyExists()
    {
        var existing = new User("taxista01", "hash");
        _users.GetByUsernameAsync("taxista01", Arg.Any<CancellationToken>()).Returns(existing);

        var request = new CreateUserRequest("taxista01", "senha123");
        var result = await _sut.CreateAsync(request);

        result.Status.Should().Be(CreateUserStatus.UsernameAlreadyExists);
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Create returns InvalidRole and does not persist when role is invalid")]
    public async Task CreateAsync_WithInvalidRole_ReturnsInvalidRole()
    {
        var request = new CreateUserRequest("taxista01", "senha123", "Gerente");
        var result = await _sut.CreateAsync(request);

        result.Status.Should().Be(CreateUserStatus.InvalidRole);
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // ActivateAsync / DeactivateAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Activate returns false when user does not exist")]
    public async Task ActivateAsync_WhenUserNotFound_ReturnsFalse()
    {
        _users.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.ActivateAsync(99);

        result.Should().BeFalse();
        await _users.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Activate activates the user and returns true when found")]
    public async Task ActivateAsync_WhenUserFound_ActivatesAndReturnsTrue()
    {
        var user = new User("taxista01", "hash");
        user.Deactivate();
        _users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.ActivateAsync(1);

        result.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Deactivate returns false when user does not exist")]
    public async Task DeactivateAsync_WhenUserNotFound_ReturnsFalse()
    {
        _users.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.DeactivateAsync(99);

        result.Should().BeFalse();
        await _users.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Deactivate deactivates the user and returns true when found")]
    public async Task DeactivateAsync_WhenUserFound_DeactivatesAndReturnsTrue()
    {
        var user = new User("taxista01", "hash");
        _users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.DeactivateAsync(1);

        result.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // UpdateProfileAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "UpdateProfile returns UserNotFound when user does not exist")]
    public async Task UpdateProfileAsync_WhenUserNotFound_ReturnsUserNotFound()
    {
        _users.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

        var request = new UpdateProfileRequest(FullName: "Novo Nome", CurrentPassword: null, NewPassword: null, Phone: null, Email: null);
        var result = await _sut.UpdateProfileAsync(99, request);

        result.Status.Should().Be(UpdateProfileStatus.UserNotFound);
    }

    [Fact(DisplayName = "UpdateProfile returns WrongPassword when current password is incorrect")]
    public async Task UpdateProfileAsync_WhenCurrentPasswordIsWrong_ReturnsWrongPassword()
    {
        var user = new User("taxista01", "hash");
        _users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("errada", "hash").Returns(false);

        var request = new UpdateProfileRequest(FullName: null, CurrentPassword: "errada", NewPassword: "nova123", Phone: null, Email: null);
        var result = await _sut.UpdateProfileAsync(1, request);

        result.Status.Should().Be(UpdateProfileStatus.WrongPassword);
    }

    [Fact(DisplayName = "UpdateProfile updates full name and returns Ok")]
    public async Task UpdateProfileAsync_WhenFullNameProvided_UpdatesAndReturnsOk()
    {
        var user = new User("taxista01", "hash");
        _users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        var request = new UpdateProfileRequest(FullName: "Novo Nome", CurrentPassword: null, NewPassword: null, Phone: null, Email: null);
        var result = await _sut.UpdateProfileAsync(1, request);

        result.Status.Should().Be(UpdateProfileStatus.Ok);
        user.FullName.Should().Be("Novo Nome");
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "UpdateProfile changes password when current password is correct")]
    public async Task UpdateProfileAsync_WhenCurrentPasswordIsCorrect_ChangesPasswordAndReturnsOk()
    {
        var user = new User("taxista01", "hash");
        _users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("correta", "hash").Returns(true);
        _hasher.Hash("nova123").Returns("novo-hash");

        var request = new UpdateProfileRequest(FullName: null, CurrentPassword: "correta", NewPassword: "nova123", Phone: null, Email: null);
        var result = await _sut.UpdateProfileAsync(1, request);

        result.Status.Should().Be(UpdateProfileStatus.Ok);
        user.PasswordHash.Should().Be("novo-hash");
    }

    // -----------------------------------------------------------------------
    // ResetPasswordAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "ResetPassword returns false when user does not exist")]
    public async Task ResetPasswordAsync_WhenUserNotFound_ReturnsFalse()
    {
        _users.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.ResetPasswordAsync(99, "novasenha");

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "ResetPassword updates password hash and returns true when user exists")]
    public async Task ResetPasswordAsync_WhenUserFound_UpdatesPasswordAndReturnsTrue()
    {
        var user = new User("taxista01", "old-hash");
        _users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Hash("novasenha").Returns("new-hash");

        var result = await _sut.ResetPasswordAsync(1, "novasenha");

        result.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }
}
