using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ReceiptGenerator.Application.Abstractions;
using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Infrastructure.Pdf;

public sealed class QuestReceiptPdfGenerator : IReceiptPdfGenerator
{
    public QuestReceiptPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(Receipt receipt)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Lato"));

                page.Header().Column(column =>
                {
                    column.Item().Text("RECIBO").Bold().FontSize(24).FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"No. {receipt.Id:000000} - Emitido em {receipt.Date:dd/MM/yyyy}");
                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Text(text =>
                    {
                        text.Span("Recebemos de ").SemiBold();
                        text.Span(receipt.Client?.Name ?? "cliente nao informado");
                        text.Span(", CPF/CNPJ ");
                        text.Span(receipt.Client?.TaxId ?? "nao informado");
                        text.Span(", o valor de ");
                        text.Span(receipt.Amount.ToString("C")).Bold();
                        text.Span(" referente a ");
                        text.Span(receipt.Description);
                        text.Span(".");
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().PaddingBottom(4).Text($"Cliente: {receipt.Client?.Name}");
                        table.Cell().PaddingBottom(4).Text($"CPF/CNPJ: {receipt.Client?.TaxId}");
                        table.Cell().PaddingBottom(4).Text($"Endereco: {receipt.Client?.Address}");
                        table.Cell().PaddingBottom(4).Text($"Valor: {receipt.Amount:C}");
                    });

                    if (!string.IsNullOrWhiteSpace(receipt.ServiceDates) || receipt.StartTime.HasValue || receipt.EndTime.HasValue)
                    {
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(details =>
                        {
                            details.Spacing(4);
                            details.Item().Text("Detalhes do servico").SemiBold();
                            AddOptional(details, "Datas", receipt.ServiceDates);
                            AddOptional(details, "Inicio", receipt.StartTime?.ToString("HH:mm"));
                            AddOptional(details, "Fim", receipt.EndTime?.ToString("HH:mm"));
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(receipt.IssuerName))
                    {
                        column.Item().Text("Emitente").SemiBold();
                        column.Item().Text(receipt.IssuerName);
                        AddOptional(column, "Telefone", receipt.IssuerPhone);
                        AddOptional(column, "Email", receipt.IssuerEmail);
                    }

                    column.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().LineHorizontal(1).LineColor(Colors.Black);
                    });
                    column.Item().Text(receipt.DriverName ?? receipt.User?.Username ?? "Responsavel").FontSize(10);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Pagina ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void AddOptional(ColumnDescriptor column, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            column.Item().Text($"{label}: {value}");
        }
    }
}
