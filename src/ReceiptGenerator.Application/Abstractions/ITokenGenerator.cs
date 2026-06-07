using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Application.Abstractions;

public interface ITokenGenerator
{
    string Generate(User user);
}
