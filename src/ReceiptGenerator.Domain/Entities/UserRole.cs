namespace ReceiptGenerator.Domain.Entities;

public static class UserRole
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Operator = "Operator";

    public static bool IsValid(string role)
    {
        return role is SuperAdmin or Operator;
    }
}
