using System.Globalization;
using Microsoft.Extensions.Options;
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
    private static readonly byte[] CooperativeLogo = LoadEmbeddedResource("coopertaxi-logo.png");

    private const string SignatureFontFamily = "Dancing Script";

    private readonly CooperativeSettings _cooperative;

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

        // Fonte cursiva para a assinatura visual do motorista
        using var sigFontStream = new MemoryStream(
            LoadEmbeddedResource("DancingScript-Regular.ttf"));
        FontManager.RegisterFont(sigFontStream);
    }

    public QuestReceiptPdfGenerator(IOptions<CooperativeSettings> cooperativeOptions)
    {
        _cooperative = cooperativeOptions.Value;
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

                if (receipt.IsCancelled)
                {
                    const string cancelledWatermark =
                        """<svg xmlns="http://www.w3.org/2000/svg" width="595" height="842">""" +
                        """<text x="297" y="421" transform="rotate(-45,297,421)" """ +
                        """text-anchor="middle" dominant-baseline="middle" """ +
                        """font-size="88" font-weight="bold" fill="#CC0000" opacity="0.18">CANCELADO</text>""" +
                        """</svg>""";

                    page.Foreground().Svg(cancelledWatermark);
                }

                page.Content().Border(1).BorderColor("#b8d7d5").Padding(22).Column(column =>
                {
                    column.Spacing(14);

                    // ── CABEÇALHO ────────────────────────────────────────────
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(72).Height(72).Image(CooperativeLogo).FitArea();

                        row.RelativeItem().PaddingLeft(14).AlignMiddle().Column(identity =>
                        {
                            identity.Item().Text(_cooperative.Name)
                                .Bold().FontSize(15).FontColor("#006f72");
                            identity.Item().PaddingTop(3).Text(_cooperative.LegalName)
                                .FontSize(8).FontColor(Colors.Grey.Darken2);
                            identity.Item().PaddingTop(2)
                                .Text($"CNPJ {_cooperative.TaxId}  ·  {_cooperative.Phone}")
                                .FontSize(8).FontColor(Colors.Grey.Darken1);
                            identity.Item().PaddingTop(1)
                                .Text(_cooperative.Email)
                                .FontSize(8).FontColor("#006f72");
                        });

                        row.ConstantItem(150).AlignBottom().Column(docTitle =>
                        {
                            docTitle.Item().AlignRight().Text("RECIBO")
                                .Bold().FontSize(26).FontColor(Colors.Grey.Darken4);
                            docTitle.Item().AlignRight()
                                .Text($"Nº {receipt.Number:000000} / {issuedAt:yyyy}")
                                .Bold().FontSize(11).FontColor("#006f72");
                        });
                    });

                    // ── LINHA AMARELA ─────────────────────────────────────────
                    column.Item().LineHorizontal(2).LineColor("#f2d500");

                    // ── VALOR ────────────────────────────────────────────────
                    column.Item().Row(valRow =>
                    {
                        valRow.RelativeItem();
                        valRow.AutoItem().Background("#006f72").PaddingVertical(10).PaddingHorizontal(20).Column(valCol =>
                        {
                            valCol.Item().AlignRight().Text("VALOR RECEBIDO")
                                .FontSize(7).Bold().FontColor("#a8d8d6");
                            valCol.Item().PaddingTop(4).AlignRight()
                                .Text(receipt.Amount.ToString("C", Brazil))
                                .Bold().FontSize(20).FontColor(Colors.White);
                        });
                    });

                    // ── DECLARAÇÃO ────────────────────────────────────────────
                    column.Item().Text(text =>
                    {
                        var clientName = receipt.Client?.Name;
                        var taxId = receipt.PayerTaxId ?? receipt.Client?.TaxId;

                        if (!string.IsNullOrWhiteSpace(clientName))
                        {
                            text.Span("Recebemos de ");
                            text.Span(clientName).Bold();

                            if (!string.IsNullOrWhiteSpace(taxId))
                            {
                                text.Span(", inscrito(a) no CPF/CNPJ sob nº ");
                                text.Span(taxId).Bold();
                            }

                            text.Span(", o valor de ");
                        }
                        else
                        {
                            text.Span("Recebemos o valor de ").SemiBold();
                        }

                        text.Span(receipt.Amount.ToString("C", Brazil)).Bold();
                        text.Span($" ({amountText}).");
                    });

                    // ── DETALHES DO SERVIÇO ───────────────────────────────────
                    column.Item().Background("#f4f8f8").Border(1).BorderColor("#b8d7d5").Padding(12).Column(details =>
                    {
                        details.Spacing(6);
                        details.Item().Text("REFERENTE A")
                            .SemiBold().FontSize(9).FontColor("#006f72");
                        details.Item().Text(receipt.Description)
                            .Bold().FontSize(12);

                        var dateRange = FormatServiceDateRange(receipt);
                        var serviceTime = FormatServiceTime(receipt);
                        if (dateRange is not null || serviceTime is not null)
                        {
                            details.Item().Row(dateRow =>
                            {
                                if (dateRange is not null)
                                    dateRow.RelativeItem()
                                        .Text($"Data(s) do serviço: {dateRange}")
                                        .FontSize(10);
                                if (serviceTime is not null)
                                    dateRow.RelativeItem()
                                        .Text($"Horário: {serviceTime}")
                                        .FontSize(10);
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(receipt.Client?.Address))
                            details.Item().Text($"Endereço: {receipt.Client.Address}").FontSize(10);
                    });

                    // ── LOCAL E DATA ──────────────────────────────────────────
                    column.Item().AlignRight()
                        .Text($"{_cooperative.City}, {issuedAt:dd} de {issuedAt.ToString("MMMM", Brazil)} de {issuedAt:yyyy}.")
                        .FontSize(10).Italic();

                    // ── ASSINATURA ────────────────────────────────────────────
                    var sigName = receipt.DriverName ?? receipt.IssuerName;
                    column.Item().PaddingTop(14).Row(sigRow =>
                    {
                        sigRow.ConstantItem(60);
                        sigRow.RelativeItem().Column(sig =>
                        {
                            // Nome em cursiva acima da linha — representa a assinatura visual
                            if (!string.IsNullOrWhiteSpace(sigName))
                            {
                                sig.Item().PaddingBottom(6).AlignCenter()
                                    .Text(sigName)
                                    .FontFamily(SignatureFontFamily)
                                    .FontSize(26)
                                    .FontColor(Colors.Grey.Darken4);
                            }

                            sig.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);

                            // Identificação impressa abaixo da linha
                            sig.Item().PaddingTop(5).AlignCenter()
                                .Text(sigName ?? "Taxista / responsável")
                                .SemiBold().FontSize(10);
                            sig.Item().AlignCenter()
                                .Text("Motorista")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrWhiteSpace(receipt.IssuerPhone))
                                sig.Item().AlignCenter()
                                    .Text(receipt.IssuerPhone)
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrWhiteSpace(receipt.IssuerEmail))
                                sig.Item().AlignCenter()
                                    .Text(receipt.IssuerEmail)
                                    .FontSize(9).FontColor("#006f72");
                        });
                        sigRow.ConstantItem(60);
                    });

                    // ── RODAPÉ ────────────────────────────────────────────────
                    column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    column.Item().AlignCenter()
                        .Text($"Documento emitido eletronicamente  ·  {issuedAt:dd/MM/yyyy}")
                        .FontSize(7).FontColor(Colors.Grey.Darken1).Italic();
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

    private static string? FormatServiceDateRange(Receipt receipt)
    {
        if (receipt.ServiceStartDate.HasValue)
        {
            var start = receipt.ServiceStartDate.Value.ToString("dd/MM/yyyy");
            if (receipt.ServiceEndDate.HasValue && receipt.ServiceEndDate != receipt.ServiceStartDate)
                return $"{start} a {receipt.ServiceEndDate.Value:dd/MM/yyyy}";
            return start;
        }

        // fallback para recibos antigos com serviceDates em texto livre
        return string.IsNullOrWhiteSpace(receipt.ServiceDates) ? null : receipt.ServiceDates;
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

    private static byte[] LoadEmbeddedResource(string fileName)
    {
        var resourceName = $"ReceiptGenerator.Infrastructure.Pdf.Assets.{fileName}";
        using var stream = typeof(QuestReceiptPdfGenerator).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
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
        var millionText = millions == 1 ? "um milhão" : $"{NumberToWords(millions)} milhões";
        return remainder == 0 ? millionText : $"{millionText} e {NumberToWords(remainder)}";
    }

    private static string HundredsToWords(int number)
    {
        string[] units = ["", "um", "dois", "três", "quatro", "cinco", "seis", "sete", "oito", "nove"];
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
