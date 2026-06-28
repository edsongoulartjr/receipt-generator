namespace ReceiptGenerator.Domain.Entities;

public static class UserRole
{
    public const string SystemAdmin = "SystemAdmin";
    public const string CoopAdmin = "CoopAdmin";
    public const string Driver = "Driver";

    public static bool IsValid(string role) =>
        role is SystemAdmin or CoopAdmin or Driver;

    public static bool IsAdmin(string role) =>
        role is SystemAdmin or CoopAdmin;
}
