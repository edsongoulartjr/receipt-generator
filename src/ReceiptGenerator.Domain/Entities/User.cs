namespace ReceiptGenerator.Domain.Entities;

public sealed class User
{
    private User()
    {
        Username = string.Empty;
        PasswordHash = string.Empty;
        FullName = string.Empty;
        Role = UserRole.Driver;
        IsActive = true;
    }

    public User(string username, string passwordHash, string role = UserRole.Driver, string? fullName = null)
    {
        Role = UserRole.Driver;
        Username = Required(username, nameof(username), 100);
        PasswordHash = Required(passwordHash, nameof(passwordHash), 500);
        FullName = string.Empty;
        ChangeRole(role);
        SetFullName(fullName);
        IsActive = true;
    }

    public int Id { get; private set; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public string FullName { get; private set; }
    public string Role { get; private set; }
    public bool IsActive { get; private set; }
    public string? RefreshTokenHash { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }

    public void ChangeRole(string role)
    {
        Role = UserRole.IsValid(role)
            ? role
            : throw new ArgumentException("Invalid user role.", nameof(role));
    }

    public void SetFullName(string? fullName)
    {
        var trimmed = fullName?.Trim() ?? string.Empty;
        FullName = trimmed.Length > 200
            ? throw new ArgumentException("FullName must have at most 200 characters.", nameof(fullName))
            : trimmed;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void SetRefreshToken(string tokenHash, DateTime expiry)
    {
        RefreshTokenHash = tokenHash;
        RefreshTokenExpiry = expiry;
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        PasswordHash = Required(newPasswordHash, nameof(newPasswordHash), 500);
        ClearRefreshToken();
    }

    public void ClearRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiry = null;
    }

    public bool HasValidRefreshToken(string tokenHash) =>
        RefreshTokenHash == tokenHash
        && RefreshTokenExpiry.HasValue
        && RefreshTokenExpiry.Value > DateTime.UtcNow;

    private static string Required(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        value = value.Trim();
        return value.Length > maxLength
            ? throw new ArgumentException($"{fieldName} must have at most {maxLength} characters.", fieldName)
            : value;
    }
}
