namespace ReceiptGenerator.Domain.Entities;

public sealed class User
{
    private User()
    {
        Username = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(string username, string passwordHash)
    {
        Username = Required(username, nameof(username), 100);
        PasswordHash = Required(passwordHash, nameof(passwordHash), 500);
    }

    public int Id { get; private set; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }

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
