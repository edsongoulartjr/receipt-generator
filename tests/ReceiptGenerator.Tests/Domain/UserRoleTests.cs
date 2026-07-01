using AwesomeAssertions;
using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Tests.Domain;

public sealed class UserRoleTests
{
    [Theory(DisplayName = "IsValid returns true for all valid roles")]
    [InlineData(UserRole.Driver)]
    [InlineData(UserRole.CoopAdmin)]
    [InlineData(UserRole.SystemAdmin)]
    public void IsValid_WithValidRole_ReturnsTrue(string role)
    {
        UserRole.IsValid(role).Should().BeTrue();
    }

    [Theory(DisplayName = "IsValid returns false for invalid or unknown roles")]
    [InlineData("")]
    [InlineData("admin")]
    [InlineData("Gerente")]
    [InlineData("driver")]  // case sensitive
    [InlineData("DRIVER")]
    public void IsValid_WithInvalidRole_ReturnsFalse(string role)
    {
        UserRole.IsValid(role).Should().BeFalse();
    }

    [Theory(DisplayName = "IsAdmin returns true only for SystemAdmin and CoopAdmin")]
    [InlineData(UserRole.SystemAdmin)]
    [InlineData(UserRole.CoopAdmin)]
    public void IsAdmin_WithAdminRole_ReturnsTrue(string role)
    {
        UserRole.IsAdmin(role).Should().BeTrue();
    }

    [Fact(DisplayName = "IsAdmin returns false for Driver role")]
    public void IsAdmin_WithDriverRole_ReturnsFalse()
    {
        UserRole.IsAdmin(UserRole.Driver).Should().BeFalse();
    }

    [Fact(DisplayName = "IsAdmin returns false for unknown roles")]
    public void IsAdmin_WithUnknownRole_ReturnsFalse()
    {
        UserRole.IsAdmin("Gerente").Should().BeFalse();
    }
}
