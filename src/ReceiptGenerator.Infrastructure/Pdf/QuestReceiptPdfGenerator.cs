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
    private static readonly byte[] CooperativeLogo = LoadEmbeddedLogo();

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
                    column.Spacing(16);

                    column.Item().Row(row =>
                    {
                        row.ConstantItem(76).Height(76).Image(CooperativeLogo).FitArea();

                        row.RelativeItem().PaddingLeft(14).AlignMiddle().Column(header =>
                        {
                            header.Item().Text("COOPERTÁXI JUNDIAÍ").Bold().FontSize(17).FontColor("#006f72");
                            header.Item().PaddingTop(3).Text("RECIBO").Bold().FontSize(27).FontColor(Colors.Grey.Darken4);
                            header.Item().Text($"Nº {receipt.Id:000000}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                        });

                        row.ConstantItem(170).Border(1).BorderColor("#008f8c").Padding(10).AlignMiddle().Column(value =>
                        {
                            value.Item().Text("VALOR").FontSize(9).FontColor(Colors.Grey.Darken1);
                            value.Item().Text(receipt.Amount.ToString("C", Brazil)).Bold().FontSize(20).FontColor("#006f72");
                        });
                    });

                    column.Item().LineHorizontal(2).LineColor("#f2d500");

                    column.Item().Text(text =>
                    {
                        text.Span("Recebemos de ").SemiBold();
                        text.Span(receipt.Client?.Name ?? "cliente não informado").Bold();
                        text.Span(", inscrito(a) no CPF/CNPJ sob nº ");
                        text.Span(receipt.Client?.TaxId ?? "não informado").Bold();
                        text.Span(", o valor de ");
                        text.Span(receipt.Amount.ToString("C", Brazil)).Bold();
                        text.Span(" (");
                        text.Span(amountText).Bold();
                        text.Span(").");
                    });

                    column.Item().Background("#f4f8f8").Border(1).BorderColor("#b8d7d5").Padding(12).Column(details =>
                    {
                        details.Spacing(5);
                        details.Item().Text("REFERENTE A").SemiBold().FontSize(9).FontColor("#006f72");
                        details.Item().Text(receipt.Description).Bold().FontSize(12);
                        AddOptional(details, "Endereço do cliente", receipt.Client?.Address);
                        AddOptional(details, "Data(s) do serviço", receipt.ServiceDates);

                        var serviceTime = FormatServiceTime(receipt);
                        AddOptional(details, "Horário", serviceTime);
                    });

                    column.Item().AlignRight().Text($"Jundiaí, {issuedAt:dd} de {issuedAt.ToString("MMMM", Brazil)} de {issuedAt:yyyy}.")
                        .FontSize(11);

                    column.Item().PaddingTop(18).Row(row =>
                    {
                        row.ConstantItem(80);
                        row.RelativeItem().Column(signature =>
                        {
                            signature.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                            signature.Item().PaddingTop(4).AlignCenter()
                                .Text(receipt.DriverName ?? "Taxista / responsável").FontSize(10);
                        });
                        row.ConstantItem(80);
                    });

                    column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    column.Item().AlignCenter().Column(footer =>
                    {
                        footer.Spacing(2);
                        footer.Item().Text("Cooperativa de Trabalho dos Taxistas de Jundiaí - SP").Bold().FontSize(10);
                        footer.Item().Text("CNPJ 44.327.517/0001-65  |  (11) 97474-9974").FontSize(9);
                        footer.Item().Text("faleconosco@coopertaxijundiaisp.com.br").FontSize(9).FontColor("#006f72");
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

    private static string? FormatServiceTime(Receipt receipt)
    {
        var start = receipt.StartTime?.ToLocalTime().ToString("HH:mm", Brazil);
        var end = receipt.EndTime?.ToLocalTime().ToString("HH:mm", Brazil);

        return (start, end) switch
        {
            (not null, not null) => $"{start} às {end}",
            (not null, null) => $"a partir das {start}",
            (null, not null) => $"até {end}",
            _ => null
        };
    }

    private static byte[] LoadEmbeddedLogo()
    {
        const string resourceName = "ReceiptGenerator.Infrastructure.Pdf.Assets.coopertaxi-logo.png";
        using var stream = typeof(QuestReceiptPdfGenerator).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded logo '{resourceName}' was not found.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
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
