using AwesomeAssertions;
using NSubstitute;
using ReceiptGenerator.Application.Abstractions;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Services;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Tests.Application;

public sealed class ReceiptServiceTests
{
    private readonly IReceiptRepository _receipts = Substitute.For<IReceiptRepository>();
    private readonly IClientRepository _clients = Substitute.For<IClientRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IReceiptPdfGenerator _pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
    private readonly ReceiptService _sut;

    public ReceiptServiceTests()
    {
        _sut = new ReceiptService(_receipts, _clients, _users, _pdfGenerator);
    }

    // -----------------------------------------------------------------------
    // GetByUserIdAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "GetByUserId returns a paged response with mapped receipt DTOs")]
    public async Task GetByUserIdAsync_ReturnsPaginatedMappedReceipts()
    {
        var list = new List<Receipt>
        {
            new(1, 10, "Corrida A", 50m),
            new(2, 10, "Corrida B", 80m)
        };
        _receipts.GetByUserIdAsync(10, 1, 20, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Receipt>)list, 2));

        var result = await _sut.GetByUserIdAsync(10, 1, 20);

        result.Items.Should().HaveCount(2);
        result.Items[0].Description.Should().Be("Corrida A");
        result.Items[1].Description.Should().Be("Corrida B");
        result.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(1);
    }

    [Fact(DisplayName = "GetByUserId returns an empty paged response when the user has no receipts")]
    public async Task GetByUserIdAsync_WhenNoReceipts_ReturnsEmptyPagedResponse()
    {
        _receipts.GetByUserIdAsync(99, 1, 20, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Receipt>)new List<Receipt>(), 0));

        var result = await _sut.GetByUserIdAsync(99, 1, 20);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "GetById returns null when receipt does not exist")]
    public async Task GetByIdAsync_WhenReceiptNotFound_ReturnsNull()
    {
        _receipts.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((Receipt?)null);

        var result = await _sut.GetByIdAsync(1, 10);

        result.Should().BeNull();
    }

    [Fact(DisplayName = "GetById returns a mapped receipt response when found")]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedResponse()
    {
        var receipt = new Receipt(1, 10, "Corrida", 75m);
        _receipts.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(receipt);

        var result = await _sut.GetByIdAsync(1, 10);

        result.Should().NotBeNull();
        result!.Description.Should().Be("Corrida");
        result.Amount.Should().Be(75m);
    }

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Create returns null when the referenced client does not belong to the user")]
    public async Task CreateAsync_WhenClientNotFound_ReturnsNull()
    {
        var driver = new User("motorista", "hash", UserRole.Driver, "Motorista Teste");
        _users.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(driver);
        _clients.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        var request = new ReceiptRequest(1, "Corrida", 50m, null, null, null, null, null, null, null);
        var result = await _sut.CreateAsync(10, UserRole.Driver, request);

        result.Should().BeNull();
        await _receipts.DidNotReceive().AddAsync(Arg.Any<Receipt>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Create persists the receipt and returns a response when data is valid")]
    public async Task CreateAsync_WithValidData_AddsReceiptAndReturnsResponse()
    {
        var driver = new User("motorista", "hash", UserRole.Driver, "Motorista Teste");
        var client = new Client("Empresa ABC", "", "", 10);
        _users.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(driver);
        _clients.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(client);
        _receipts.GetNextNumberAsync(10, Arg.Any<CancellationToken>())
            .Returns(1);
        _receipts.GetByIdAndUserIdAsync(Arg.Any<int>(), 10, Arg.Any<CancellationToken>())
            .Returns((Receipt?)null);

        var request = new ReceiptRequest(1, "Transporte executivo", 200m, null, null, null, null, null, null, null);
        var result = await _sut.CreateAsync(10, UserRole.Driver, request);

        await _receipts.Received(1).AddAsync(Arg.Any<Receipt>(), Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
        result!.Description.Should().Be("Transporte executivo");
        result.Amount.Should().Be(200m);
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Update returns false when the receipt does not exist")]
    public async Task UpdateAsync_WhenReceiptNotFound_ReturnsFalse()
    {
        _receipts.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((Receipt?)null);

        var request = new ReceiptRequest(1, "Corrida", 50m, null, null, null, null, null, null, null);
        var result = await _sut.UpdateAsync(1, 10, request);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Update returns false when the referenced client does not belong to the user")]
    public async Task UpdateAsync_WhenClientNotFound_ReturnsFalse()
    {
        var receipt = new Receipt(1, 10, "Corrida", 50m);
        _receipts.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(receipt);
        _clients.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        var request = new ReceiptRequest(1, "Corrida", 50m, null, null, null, null, null, null, null);
        var result = await _sut.UpdateAsync(1, 10, request);

        result.Should().BeFalse();
        await _receipts.DidNotReceive().UpdateAsync(Arg.Any<Receipt>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Update persists changes and returns true when data is valid")]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsTrue()
    {
        var receipt = new Receipt(1, 10, "Original", 50m);
        var client = new Client("Empresa", "", "", 10);
        _receipts.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(receipt);
        _clients.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(client);

        var request = new ReceiptRequest(1, "Atualizado", 300m, null, null, null, null, null, null, null);
        var result = await _sut.UpdateAsync(1, 10, request);

        result.Should().BeTrue();
        await _receipts.Received(1).UpdateAsync(receipt, Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Delete returns false when the receipt does not exist")]
    public async Task DeleteAsync_WhenReceiptNotFound_ReturnsFalse()
    {
        _receipts.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((Receipt?)null);

        var result = await _sut.DeleteAsync(1, 10);

        result.Should().BeFalse();
        await _receipts.DidNotReceive().DeleteAsync(Arg.Any<Receipt>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Delete removes the receipt and returns true when found")]
    public async Task DeleteAsync_WhenReceiptFound_DeletesAndReturnsTrue()
    {
        var receipt = new Receipt(1, 10, "Corrida", 50m);
        _receipts.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(receipt);

        var result = await _sut.DeleteAsync(1, 10);

        result.Should().BeTrue();
        await _receipts.Received(1).DeleteAsync(receipt, Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // GeneratePdfAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "GeneratePdf returns null when the receipt does not exist")]
    public async Task GeneratePdfAsync_WhenReceiptNotFound_ReturnsNull()
    {
        _receipts.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((Receipt?)null);

        var result = await _sut.GeneratePdfAsync(1, 10);

        result.Should().BeNull();
        _pdfGenerator.DidNotReceive().Generate(Arg.Any<Receipt>());
    }

    [Fact(DisplayName = "GeneratePdf returns PDF bytes when the receipt is found")]
    public async Task GeneratePdfAsync_WhenReceiptFound_ReturnsPdfBytes()
    {
        var receipt = new Receipt(1, 10, "Corrida", 50m);
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        _receipts.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(receipt);
        _pdfGenerator.Generate(receipt).Returns(pdfBytes);

        var result = await _sut.GeneratePdfAsync(1, 10);

        result.Should().BeEquivalentTo(pdfBytes);
        _pdfGenerator.Received(1).Generate(receipt);
    }
}
