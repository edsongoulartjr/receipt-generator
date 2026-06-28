using System.Security.Cryptography;
using System.Text;
using ReceiptGenerator.Application.Abstractions;

namespace ReceiptGenerator.Infrastructure.Security;

public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    // 64 bytes = 512 bits de entropia; base64url sem padding
    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    // SHA-256 do token para armazenamento — rápido o suficiente para lookup frequente
    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
