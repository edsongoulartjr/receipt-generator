using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Application.Abstractions;

public interface IReceiptPdfGenerator
{
    byte[] Generate(Receipt receipt);
}
