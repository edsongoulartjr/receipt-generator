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
        _receipts.GetByUserIdAsync(10, 1, 20, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
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
        _receipts.GetByUserIdAsync(99, 1, 20, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
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
        await _receipts.DidNotReceive().CancelAsync(Arg.Any<Receipt>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Delete removes the receipt and returns true when found")]
    public async Task DeleteAsync_WhenReceiptFound_DeletesAndReturnsTrue()
    {
        var receipt = new Receipt(1, 10, "Corrida", 50m);
        _receipts.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(receipt);

        var result = await _sut.DeleteAsync(1, 10);

        result.Should().BeTrue();
        await _receipts.Received(1).CancelAsync(receipt, Arg.Any<string?>(), Arg.Any<CancellationToken>());
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

    // -----------------------------------------------------------------------
    // CreateAsync — driver inativo e admin emitindo por outro driver
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Create returns null when the driver user does not exist")]
    public async Task CreateAsync_WhenDriverNotFound_ReturnsNull()
    {
        _users.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns((User?)null);

        var request = new ReceiptRequest(null, "Corrida", 50m, null, null, null, null, null, null, null);
        var result = await _sut.CreateAsync(10, UserRole.Driver, request);

        result.Should().BeNull();
        await _receipts.DidNotReceive().AddAsync(Arg.Any<Receipt>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Create returns null when the driver user is inactive")]
    public async Task CreateAsync_WhenDriverIsInactive_ReturnsNull()
    {
        var driver = new User("motorista", "hash", UserRole.Driver, "Carlos");
        driver.Deactivate();
        _users.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(driver);

        var request = new ReceiptRequest(null, "Corrida", 50m, null, null, null, null, null, null, null);
        var result = await _sut.CreateAsync(10, UserRole.Driver, request);

        result.Should().BeNull();
        await _receipts.DidNotReceive().AddAsync(Arg.Any<Receipt>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Create uses DriverUserId when admin emits receipt on behalf of another driver")]
    public async Task CreateAsync_WhenAdminSpecifiesDriverUserId_UsesDriverUserIdToCreateReceipt()
    {
        var driver = new User("motorista", "hash", UserRole.Driver, "Carlos");
        var client = new Client("Empresa", "", "", 20);
        _users.GetByIdAsync(20, Arg.Any<CancellationToken>()).Returns(driver);
        _clients.GetByIdAndUserIdAsync(1, 20, Arg.Any<CancellationToken>()).Returns(client);
        _receipts.GetNextNumberAsync(20, Arg.Any<CancellationToken>()).Returns(1);
        _receipts.GetByIdAndUserIdAsync(Arg.Any<int>(), 20, Arg.Any<CancellationToken>())
            .Returns((Receipt?)null);

        // Admin (userId=99) emite em nome do motorista (driverUserId=20)
        var request = new ReceiptRequest(1, "Corrida admin", 100m, null, null, null, null, null, null, null, DriverUserId: 20);
        var result = await _sut.CreateAsync(99, UserRole.CoopAdmin, request);

        result.Should().NotBeNull();
        // Verifica que o repositório foi chamado para adicionar com o userId do motorista
        await _receipts.Received(1).AddAsync(
            Arg.Is<Receipt>(r => r.UserId == 20),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Create ignores DriverUserId when requesting user is a Driver")]
    public async Task CreateAsync_WhenDriverSpecifiesDriverUserId_UsesOwnUserId()
    {
        var driver = new User("motorista", "hash", UserRole.Driver, "Carlos");
        _users.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(driver);
        _receipts.GetNextNumberAsync(10, Arg.Any<CancellationToken>()).Returns(1);
        _receipts.GetByIdAndUserIdAsync(Arg.Any<int>(), 10, Arg.Any<CancellationToken>())
            .Returns((Receipt?)null);

        // Driver tenta especificar outro driverUserId=99, deve ser ignorado
        var request = new ReceiptRequest(null, "Corrida", 50m, null, null, null, null, null, null, null, DriverUserId: 99);
        var result = await _sut.CreateAsync(10, UserRole.Driver, request);

        result.Should().NotBeNull();
        await _receipts.Received(1).AddAsync(
            Arg.Is<Receipt>(r => r.UserId == 10),
            Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // DeleteAsync — recibo já cancelado
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Delete returns false when the receipt is already cancelled")]
    public async Task DeleteAsync_WhenReceiptIsAlreadyCancelled_ReturnsFalse()
    {
        var receipt = new Receipt(1, 10, "Corrida", 50m);
        receipt.Cancel("motivo original");
        _receipts.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>()).Returns(receipt);

        var result = await _sut.DeleteAsync(1, 10);

        result.Should().BeFalse();
        await _receipts.DidNotReceive().CancelAsync(Arg.Any<Receipt>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // Map — client null vs client preenchido
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "GetById returns response with null client when receipt has no client")]
    public async Task GetByIdAsync_WhenReceiptHasNoClient_ReturnsResponseWithNullClient()
    {
        var receipt = new Receipt(clientId: null, userId: 10, description: "Avulso", amount: 50m);
        _receipts.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>()).Returns(receipt);

        var result = await _sut.GetByIdAsync(1, 10);

        result.Should().NotBeNull();
        result!.Client.Should().BeNull();
    }

    [Fact(DisplayName = "GetById returns response with stub client when ClientId is set but Client navigation is null")]
    public async Task GetByIdAsync_WhenClientIdSetButNavigationNull_ReturnsStubClient()
    {
        // Simula o caso em que o ORM não carregou a navegação (Client == null) mas ClientId está preenchido
        var receipt = new Receipt(clientId: 5, userId: 10, description: "Corrida", amount: 50m);
        _receipts.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>()).Returns(receipt);

        var result = await _sut.GetByIdAsync(1, 10);

        result.Should().NotBeNull();
        result!.Client.Should().NotBeNull();
        result.Client!.Id.Should().Be(5);
        result.Client.Name.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // Paginação — TotalPages
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "GetByUserId calculates TotalPages correctly for multiple pages")]
    public async Task GetByUserIdAsync_WithMultiplePages_CalculatesTotalPagesCorrectly()
    {
        var list = new List<Receipt> { new(1, 10, "Corrida", 50m) };
        _receipts.GetByUserIdAsync(10, 1, 5, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Receipt>)list, 11));

        var result = await _sut.GetByUserIdAsync(10, 1, 5);

        result.TotalPages.Should().Be(3); // ceil(11/5) = 3
        result.TotalCount.Should().Be(11);
    }

    [Fact(DisplayName = "GetByUserId returns TotalPages of 1 when total count is zero")]
    public async Task GetByUserIdAsync_WhenTotalCountIsZero_ReturnsTotalPagesOfOne()
    {
        _receipts.GetByUserIdAsync(10, 1, 20, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Receipt>)new List<Receipt>(), 0));

        var result = await _sut.GetByUserIdAsync(10, 1, 20);

        result.TotalPages.Should().Be(1);
    }
}
