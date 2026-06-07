namespace ReceiptGenerator.Domain.Entities;

public sealed class User
{
    private User()
    {
        Username = string.Empty;
        PasswordHash = string.Empty;
        Role = UserRole.Operator;
        IsActive = true;
    }

    public User(string username, string passwordHash, string role = UserRole.Operator)
    {
        Role = UserRole.Operator;
        Username = Required(username, nameof(username), 100);
        PasswordHash = Required(passwordHash, nameof(passwordHash), 500);
        ChangeRole(role);
        IsActive = true;
    }

    public int Id { get; private set; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public string Role { get; private set; }
    public bool IsActive { get; private set; }

    public void ChangeRole(string role)
    {
        Role = UserRole.IsValid(role)
            ? role
            : throw new ArgumentException("Invalid user role.", nameof(role));
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

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
