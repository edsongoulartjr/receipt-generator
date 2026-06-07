using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ReceiptGenerator.Application.Abstractions;
using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Infrastructure.Security;

public sealed class JwtTokenGenerator : ITokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Generate(User user)
    {
        var secret = _configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JWT secret is not configured.");
        var key = Encoding.UTF8.GetBytes(secret);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
