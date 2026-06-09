using System.Globalization;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ReceiptGenerator.Application.Abstractions;
using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Infrastructure.Pdf;

public sealed class QuestReceiptPdfGenerator : IReceiptPdfGenerator
{
    private static readonly CultureInfo Brazil = new("pt-BR");

    static QuestReceiptPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseEnvironmentFonts = false;
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = true;

        var bundledFontsPath = Path.Combine(
            AppContext.BaseDirectory,
            "runtimes",
            "any",
            "native",
            "LatoFont");

        if (!Directory.Exists(bundledFontsPath))
        {
            throw new DirectoryNotFoundException(
                $"QuestPDF bundled fonts were not found at '{bundledFontsPath}'.");
        }

        foreach (var fontFile in Directory.EnumerateFiles(bundledFontsPath, "*.ttf"))
        {
            using var fontStream = File.OpenRead(fontFile);
            FontManager.RegisterFont(fontStream);
        }
    }

    public byte[] Generate(Receipt receipt)
    {
        var issuedAt = receipt.Date.ToLocalTime();
        var amountText = AmountToWords(receipt.Amount);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Lato"));

                page.Content().Border(1).BorderColor(Colors.Grey.Darken2).Padding(22).Column(column =>
                {
                    column.Spacing(18);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(header =>
                        {
                            header.Item().Text("RECIBO").Bold().FontSize(30).FontColor(Colors.Grey.Darken4);
                            header.Item().Text($"COOPERTAXI JUNDIAI | No. {receipt.Id:000000}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                        });

                        row.ConstantItem(175).Border(1).BorderColor(Colors.Grey.Darken2).Padding(10).Column(value =>
                        {
                            value.Item().Text("VALOR").FontSize(9).FontColor(Colors.Grey.Darken1);
                            value.Item().Text(receipt.Amount.ToString("C", Brazil)).Bold().FontSize(20);
                        });
                    });

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    column.Item().Text(text =>
                    {
                        text.Span("Recebi(emos) de ").SemiBold();
                        text.Span(receipt.Client?.Name ?? "cliente nao informado").Bold();
                        text.Span(", CPF/CNPJ ");
                        text.Span(receipt.Client?.TaxId ?? "nao informado").Bold();
                        text.Span(", a importancia de ");
                        text.Span(receipt.Amount.ToString("C", Brazil)).Bold();
                        text.Span(" (");
                        text.Span(amountText).Bold();
                        text.Span("), referente a ");
                        text.Span(receipt.Description).Bold();
                        text.Span(".");
                    });

                    column.Item().Background(Colors.Grey.Lighten5).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(details =>
                    {
                        details.Spacing(6);
                        details.Item().Text("Detalhes do servico").SemiBold().FontSize(12);
                        details.Item().Text($"Cliente: {receipt.Client?.Name ?? "nao informado"}");
                        details.Item().Text($"CPF/CNPJ: {receipt.Client?.TaxId ?? "nao informado"}");
                        details.Item().Text($"Endereco: {receipt.Client?.Address ?? "nao informado"}");
                        AddOptional(details, "Datas", receipt.ServiceDates);
                        AddOptional(details, "Inicio", receipt.StartTime?.ToLocalTime().ToString("HH:mm", Brazil));
                        AddOptional(details, "Fim", receipt.EndTime?.ToLocalTime().ToString("HH:mm", Brazil));
                    });

                    column.Item().AlignRight().Text($"Jundiai, {issuedAt:dd} de {issuedAt.ToString("MMMM", Brazil)} de {issuedAt:yyyy}.")
                        .FontSize(11);

                    column.Item().Background("#fff8d8").Border(1).BorderColor("#e6c300").Padding(12).Column(coop =>
                    {
                        coop.Spacing(3);
                        coop.Item().Text("COOPERTAXI JUNDIAI - (11) 97474-9974").Bold();
                        coop.Item().Text("Cooperativa de Trabalho dos Taxistas de Jundiai - SP");
                        coop.Item().Text("CNPJ 44.327.517/0001-65");
                        coop.Item().Text("faleconosco@coopertaxijundiaisp.com.br");
                    });

                    column.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().Column(signature =>
                        {
                            signature.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                            signature.Item().PaddingTop(4).Text(receipt.DriverName ?? "Nome do taxista/responsavel").FontSize(10);
                        });
                    });
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

    private static string AmountToWords(decimal amount)
    {
        var totalCents = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
        var reais = totalCents / 100;
        var cents = totalCents % 100;

        var result = reais == 0
            ? "zero real"
            : $"{NumberToWords(reais)} {(reais == 1 ? "real" : "reais")}";

        if (cents > 0)
        {
            result += $" e {NumberToWords(cents)} {(cents == 1 ? "centavo" : "centavos")}";
        }

        return result;
    }

    private static string NumberToWords(long number)
    {
        if (number == 0)
        {
            return "zero";
        }

        if (number < 1000)
        {
            return HundredsToWords((int)number);
        }

        if (number < 1_000_000)
        {
            var thousands = number / 1000;
            var rest = number % 1000;
            var text = thousands == 1 ? "mil" : $"{NumberToWords(thousands)} mil";
            return rest == 0 ? text : $"{text} e {HundredsToWords((int)rest)}";
        }

        var millions = number / 1_000_000;
        var remainder = number % 1_000_000;
        var millionText = millions == 1 ? "um milhao" : $"{NumberToWords(millions)} milhoes";
        return remainder == 0 ? millionText : $"{millionText} e {NumberToWords(remainder)}";
    }

    private static string HundredsToWords(int number)
    {
        string[] units = ["", "um", "dois", "tres", "quatro", "cinco", "seis", "sete", "oito", "nove"];
        string[] teens = ["dez", "onze", "doze", "treze", "quatorze", "quinze", "dezesseis", "dezessete", "dezoito", "dezenove"];
        string[] tens = ["", "", "vinte", "trinta", "quarenta", "cinquenta", "sessenta", "setenta", "oitenta", "noventa"];
        string[] hundreds = ["", "cento", "duzentos", "trezentos", "quatrocentos", "quinhentos", "seiscentos", "setecentos", "oitocentos", "novecentos"];

        if (number == 100)
        {
            return "cem";
        }

        if (number < 10)
        {
            return units[number];
        }

        if (number < 20)
        {
            return teens[number - 10];
        }

        if (number < 100)
        {
            var ten = number / 10;
            var unit = number % 10;
            return unit == 0 ? tens[ten] : $"{tens[ten]} e {units[unit]}";
        }

        var hundred = number / 100;
        var rest = number % 100;
        return rest == 0 ? hundreds[hundred] : $"{hundreds[hundred]} e {HundredsToWords(rest)}";
    }
}
