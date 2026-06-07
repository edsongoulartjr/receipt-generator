namespace ReceiptGenerator.Domain.Entities;

public sealed class Receipt
{
    private Receipt()
    {
        Description = string.Empty;
    }

    public Receipt(
        int clientId,
        int userId,
        string description,
        decimal amount,
        DateTime? startTime = null,
        DateTime? endTime = null,
        string? serviceDates = null,
        string? issuerName = null,
        string? issuerPhone = null,
        string? issuerEmail = null,
        string? driverName = null)
    {
        Description = string.Empty;
        ClientId = clientId > 0 ? clientId : throw new ArgumentOutOfRangeException(nameof(clientId));
        UserId = userId > 0 ? userId : throw new ArgumentOutOfRangeException(nameof(userId));
        Date = DateTime.UtcNow;
        Update(description, amount, startTime, endTime, serviceDates, issuerName, issuerPhone, issuerEmail, driverName);
    }

    public int Id { get; private set; }
    public DateTime Date { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime? StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public string? ServiceDates { get; private set; }
    public string? IssuerName { get; private set; }
    public string? IssuerPhone { get; private set; }
    public string? IssuerEmail { get; private set; }
    public string? DriverName { get; private set; }
    public int ClientId { get; private set; }
    public Client? Client { get; private set; }
    public int UserId { get; private set; }
    public User? User { get; private set; }

    public void ChangeClient(int clientId)
    {
        ClientId = clientId > 0 ? clientId : throw new ArgumentOutOfRangeException(nameof(clientId));
    }

    public void Update(
        string description,
        decimal amount,
        DateTime? startTime,
        DateTime? endTime,
        string? serviceDates,
        string? issuerName,
        string? issuerPhone,
        string? issuerEmail,
        string? driverName)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Receipt amount must be greater than zero.");
        }

        if (startTime.HasValue && endTime.HasValue && endTime < startTime)
        {
            throw new ArgumentException("End time cannot be earlier than start time.", nameof(endTime));
        }

        Description = Required(description, nameof(description), 1000);
        Amount = amount;
        StartTime = startTime;
        EndTime = endTime;
        ServiceDates = Optional(serviceDates, 100);
        IssuerName = Optional(issuerName, 200);
        IssuerPhone = Optional(issuerPhone, 50);
        IssuerEmail = Optional(issuerEmail, 200);
        DriverName = Optional(driverName, 200);
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

    private static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        return value.Length > maxLength
            ? throw new ArgumentException($"Value must have at most {maxLength} characters.")
            : value;
    }
}
