namespace ReceiptGenerator.Application.Abstractions;

public interface IRefreshTokenGenerator
{
    string Generate();
    string Hash(string token);
}
