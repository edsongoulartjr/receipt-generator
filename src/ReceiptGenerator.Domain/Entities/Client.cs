namespace ReceiptGenerator.Domain.Entities;

public sealed class Client
{
    private Client()
    {
        Name = string.Empty;
        Address = string.Empty;
        TaxId = string.Empty;
    }

    public Client(string name, string address, string taxId, int userId)
    {
        Name = string.Empty;
        Address = string.Empty;
        TaxId = string.Empty;
        Rename(name);
        ChangeAddress(address);
        ChangeTaxId(taxId);
        UserId = userId > 0 ? userId : throw new ArgumentOutOfRangeException(nameof(userId));
    }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Address { get; private set; }
    public string TaxId { get; private set; }
    public int UserId { get; private set; }
    public User? User { get; private set; }

    public void Update(string name, string address, string taxId)
    {
        Rename(name);
        ChangeAddress(address);
        ChangeTaxId(taxId);
    }

    private void Rename(string name) => Name = Required(name, nameof(name), 200);
    private void ChangeAddress(string address) => Address = Required(address, nameof(address), 500);
    private void ChangeTaxId(string taxId) => TaxId = Required(taxId, nameof(taxId), 50);

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
