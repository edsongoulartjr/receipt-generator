using AwesomeAssertions;
using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Tests.Domain;

public sealed class UserTests
{
    // -----------------------------------------------------------------------
    // Constructor — happy path
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "User is created with all fields correctly set")]
    public void Constructor_WithValidArguments_CreatesUser()
    {
        var user = new User("taxista01", "hashed-pw", UserRole.Driver, "Carlos Silva");

        user.Username.Should().Be("taxista01");
        user.PasswordHash.Should().Be("hashed-pw");
        user.Role.Should().Be(UserRole.Driver);
        user.FullName.Should().Be("Carlos Silva");
        user.IsActive.Should().BeTrue();
    }

    [Fact(DisplayName = "User is created with Driver role when no role is specified")]
    public void Constructor_WithNoRole_DefaultsToDriver()
    {
        var user = new User("taxista01", "hashed-pw");

        user.Role.Should().Be(UserRole.Driver);
    }

    [Fact(DisplayName = "User is created with empty full name when fullName is null")]
    public void Constructor_WithNullFullName_SetsEmptyFullName()
    {
        var user = new User("taxista01", "hashed-pw");

        user.FullName.Should().BeEmpty();
    }

    [Fact(DisplayName = "User username and password hash are trimmed on creation")]
    public void Constructor_TrimsUsernameAndPasswordHash()
    {
        var user = new User("  admin  ", "  hash  ");

        user.Username.Should().Be("admin");
        user.PasswordHash.Should().Be("hash");
    }

    // -----------------------------------------------------------------------
    // Constructor — validations
    // -----------------------------------------------------------------------

    [Theory(DisplayName = "User creation throws when username is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyUsername_Throws(string username)
    {
        var act = () => new User(username, "hashed-pw");

        act.Should().Throw<ArgumentException>();
    }

    [Theory(DisplayName = "User creation throws when password hash is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyPasswordHash_Throws(string passwordHash)
    {
        var act = () => new User("taxista01", passwordHash);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "User creation throws when username exceeds 100 characters")]
    public void Constructor_WithUsernameLongerThan100Chars_Throws()
    {
        var longUsername = new string('a', 101);

        var act = () => new User(longUsername, "hash");

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "User creation throws when full name exceeds 200 characters")]
    public void Constructor_WithFullNameLongerThan200Chars_Throws()
    {
        var longName = new string('a', 201);

        var act = () => new User("taxista01", "hash", UserRole.Driver, longName);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "User creation throws when role is invalid")]
    public void Constructor_WithInvalidRole_Throws()
    {
        var act = () => new User("taxista01", "hash", "SuperUser");

        act.Should().Throw<ArgumentException>();
    }

    // -----------------------------------------------------------------------
    // ChangeRole
    // -----------------------------------------------------------------------

    [Theory(DisplayName = "ChangeRole accepts all valid roles")]
    [InlineData(UserRole.Driver)]
    [InlineData(UserRole.CoopAdmin)]
    [InlineData(UserRole.SystemAdmin)]
    public void ChangeRole_WithValidRole_UpdatesRole(string role)
    {
        var user = new User("taxista01", "hash");

        user.ChangeRole(role);

        user.Role.Should().Be(role);
    }

    [Fact(DisplayName = "ChangeRole throws when role is invalid")]
    public void ChangeRole_WithInvalidRole_Throws()
    {
        var user = new User("taxista01", "hash");

        var act = () => user.ChangeRole("Gerente");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("role");
    }

    // -----------------------------------------------------------------------
    // Activate / Deactivate
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Deactivate sets IsActive to false")]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var user = new User("taxista01", "hash");

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    [Fact(DisplayName = "Activate sets IsActive to true after deactivation")]
    public void Activate_AfterDeactivation_SetsIsActiveToTrue()
    {
        var user = new User("taxista01", "hash");
        user.Deactivate();

        user.Activate();

        user.IsActive.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // SetRefreshToken / ClearRefreshToken / HasValidRefreshToken
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "SetRefreshToken stores the token hash and expiry")]
    public void SetRefreshToken_StoresHashAndExpiry()
    {
        var user = new User("taxista01", "hash");
        var expiry = DateTime.UtcNow.AddDays(30);

        user.SetRefreshToken("token-hash", expiry);

        user.RefreshTokenHash.Should().Be("token-hash");
        user.RefreshTokenExpiry.Should().Be(expiry);
    }

    [Fact(DisplayName = "HasValidRefreshToken returns true when token matches and is not expired")]
    public void HasValidRefreshToken_WhenTokenMatchesAndNotExpired_ReturnsTrue()
    {
        var user = new User("taxista01", "hash");
        user.SetRefreshToken("my-hash", DateTime.UtcNow.AddHours(1));

        user.HasValidRefreshToken("my-hash").Should().BeTrue();
    }

    [Fact(DisplayName = "HasValidRefreshToken returns false when token does not match")]
    public void HasValidRefreshToken_WhenTokenDoesNotMatch_ReturnsFalse()
    {
        var user = new User("taxista01", "hash");
        user.SetRefreshToken("correct-hash", DateTime.UtcNow.AddHours(1));

        user.HasValidRefreshToken("wrong-hash").Should().BeFalse();
    }

    [Fact(DisplayName = "HasValidRefreshToken returns false when token is expired")]
    public void HasValidRefreshToken_WhenTokenIsExpired_ReturnsFalse()
    {
        var user = new User("taxista01", "hash");
        user.SetRefreshToken("my-hash", DateTime.UtcNow.AddSeconds(-1));

        user.HasValidRefreshToken("my-hash").Should().BeFalse();
    }

    [Fact(DisplayName = "ClearRefreshToken removes the stored token hash and expiry")]
    public void ClearRefreshToken_NullifiesHashAndExpiry()
    {
        var user = new User("taxista01", "hash");
        user.SetRefreshToken("token-hash", DateTime.UtcNow.AddDays(1));

        user.ClearRefreshToken();

        user.RefreshTokenHash.Should().BeNull();
        user.RefreshTokenExpiry.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // ChangePasswordHash
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "ChangePasswordHash updates the hash and clears the refresh token")]
    public void ChangePasswordHash_UpdatesHashAndClearsRefreshToken()
    {
        var user = new User("taxista01", "old-hash");
        user.SetRefreshToken("rt-hash", DateTime.UtcNow.AddDays(30));

        user.ChangePasswordHash("new-hash");

        user.PasswordHash.Should().Be("new-hash");
        user.RefreshTokenHash.Should().BeNull();
        user.RefreshTokenExpiry.Should().BeNull();
    }

    [Theory(DisplayName = "ChangePasswordHash throws when new password hash is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangePasswordHash_WithEmptyHash_Throws(string newHash)
    {
        var user = new User("taxista01", "old-hash");

        var act = () => user.ChangePasswordHash(newHash);

        act.Should().Throw<ArgumentException>();
    }

    // -----------------------------------------------------------------------
    // SetFullName / SetPhone / SetEmail
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "SetFullName trims the name and updates FullName")]
    public void SetFullName_TrimsAndUpdatesValue()
    {
        var user = new User("taxista01", "hash");

        user.SetFullName("  Carlos Silva  ");

        user.FullName.Should().Be("Carlos Silva");
    }

    [Fact(DisplayName = "SetFullName sets FullName to empty when null is passed")]
    public void SetFullName_WithNull_SetsEmptyFullName()
    {
        var user = new User("taxista01", "hash", fullName: "Carlos");

        user.SetFullName(null);

        user.FullName.Should().BeEmpty();
    }

    [Fact(DisplayName = "SetPhone normalizes phone to null when whitespace-only is provided")]
    public void SetPhone_WithWhitespace_SetsNull()
    {
        var user = new User("taxista01", "hash");

        user.SetPhone("   ");

        user.Phone.Should().BeNull();
    }

    [Fact(DisplayName = "SetPhone stores the trimmed phone number")]
    public void SetPhone_WithValidPhone_StoresValue()
    {
        var user = new User("taxista01", "hash");

        user.SetPhone("  11999990000  ");

        user.Phone.Should().Be("11999990000");
    }

    [Fact(DisplayName = "SetEmail stores the trimmed email address")]
    public void SetEmail_WithValidEmail_StoresValue()
    {
        var user = new User("taxista01", "hash");

        user.SetEmail("  user@email.com  ");

        user.Email.Should().Be("user@email.com");
    }

    [Fact(DisplayName = "SetPhone throws when phone exceeds 50 characters")]
    public void SetPhone_WithPhoneLongerThan50Chars_Throws()
    {
        var user = new User("taxista01", "hash");
        var longPhone = new string('9', 51);

        var act = () => user.SetPhone(longPhone);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "SetEmail throws when email exceeds 200 characters")]
    public void SetEmail_WithEmailLongerThan200Chars_Throws()
    {
        var user = new User("taxista01", "hash");
        var longEmail = new string('a', 201);

        var act = () => user.SetEmail(longEmail);

        act.Should().Throw<ArgumentException>();
    }
}
