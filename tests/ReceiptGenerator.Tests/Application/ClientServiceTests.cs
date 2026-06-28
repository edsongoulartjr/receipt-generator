using AwesomeAssertions;
using NSubstitute;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Services;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Tests.Application;

public sealed class ClientServiceTests
{
    private readonly IClientRepository _clients = Substitute.For<IClientRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ClientService _sut;

    public ClientServiceTests()
    {
        _sut = new ClientService(_clients, _users);
    }

    // -----------------------------------------------------------------------
    // GetByUserIdAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "GetByUserId returns all clients mapped to response DTOs")]
    public async Task GetByUserIdAsync_ReturnsMappedClients()
    {
        var list = new List<Client>
        {
            new("Empresa A", "Rua 1", "111", 10),
            new("Empresa B", "", "", 10)
        };
        _clients.GetByUserIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(list);

        var result = await _sut.GetByUserIdAsync(10);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Empresa A");
        result[1].Name.Should().Be("Empresa B");
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "GetById returns null when client does not exist")]
    public async Task GetByIdAsync_WhenClientNotFound_ReturnsNull()
    {
        _clients.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        var result = await _sut.GetByIdAsync(1, 10);

        result.Should().BeNull();
    }

    [Fact(DisplayName = "GetById returns a mapped client response when found")]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedResponse()
    {
        var client = new Client("Empresa XYZ", "Rua A", "123", 10);
        _clients.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(client);

        var result = await _sut.GetByIdAsync(1, 10);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Empresa XYZ");
        result.Address.Should().Be("Rua A");
        result.TaxId.Should().Be("123");
    }

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Create returns null and does not persist when user does not exist")]
    public async Task CreateAsync_WhenUserNotFound_ReturnsNull()
    {
        _users.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var request = new ClientRequest("Empresa", "Rua", "123");
        var result = await _sut.CreateAsync(10, request);

        result.Should().BeNull();
        await _clients.DidNotReceive().AddAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Create persists the client and returns a response when data is valid")]
    public async Task CreateAsync_WithValidData_AddsClientAndReturnsResponse()
    {
        var user = new User("taxista01", "hash", UserRole.Operator);
        _users.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(user);

        var request = new ClientRequest("Novo Cliente", "Rua B", "456");
        var result = await _sut.CreateAsync(10, request);

        await _clients.Received(1).AddAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
        result!.Name.Should().Be("Novo Cliente");
    }

    [Fact(DisplayName = "Create persists a client with empty address and tax id when only name is provided")]
    public async Task CreateAsync_WithNameOnly_AddsClientWithEmptyAddressAndTaxId()
    {
        var user = new User("taxista01", "hash", UserRole.Operator);
        _users.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(user);

        var request = new ClientRequest("Passageiro");
        var result = await _sut.CreateAsync(10, request);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Passageiro");
        result.Address.Should().BeEmpty();
        result.TaxId.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Update returns false and does not persist when client does not exist")]
    public async Task UpdateAsync_WhenClientNotFound_ReturnsFalse()
    {
        _clients.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        var request = new ClientRequest("Nome", "", "");
        var result = await _sut.UpdateAsync(1, 10, request);

        result.Should().BeFalse();
        await _clients.DidNotReceive().UpdateAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Update persists changes and returns true when data is valid")]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsTrue()
    {
        var client = new Client("Antigo", "", "", 10);
        _clients.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(client);

        var request = new ClientRequest("Atualizado", "Nova Rua", "999");
        var result = await _sut.UpdateAsync(1, 10, request);

        result.Should().BeTrue();
        await _clients.Received(1).UpdateAsync(client, Arg.Any<CancellationToken>());
        client.Name.Should().Be("Atualizado");
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Delete returns false and does not remove when client does not exist")]
    public async Task DeleteAsync_WhenClientNotFound_ReturnsFalse()
    {
        _clients.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        var result = await _sut.DeleteAsync(1, 10);

        result.Should().BeFalse();
        await _clients.DidNotReceive().DeleteAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Delete removes the client and returns true when found")]
    public async Task DeleteAsync_WhenFound_DeletesAndReturnsTrue()
    {
        var client = new Client("Empresa", "", "", 10);
        _clients.GetByIdAndUserIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(client);

        var result = await _sut.DeleteAsync(1, 10);

        result.Should().BeTrue();
        await _clients.Received(1).DeleteAsync(client, Arg.Any<CancellationToken>());
    }
}
