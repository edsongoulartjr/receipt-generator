using AwesomeAssertions;
using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Tests.Domain;

public sealed class ClientTests
{
    // -----------------------------------------------------------------------
    // Constructor — happy path
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Client is created with all fields correctly set")]
    public void Constructor_WithValidArguments_CreatesClient()
    {
        var client = new Client("Empresa XYZ", "Rua A, 100", "12.345.678/0001-99", userId: 1);

        client.Name.Should().Be("Empresa XYZ");
        client.Address.Should().Be("Rua A, 100");
        client.TaxId.Should().Be("12.345.678/0001-99");
        client.UserId.Should().Be(1);
    }

    [Fact(DisplayName = "Client name, address and tax id are trimmed of surrounding spaces on creation")]
    public void Constructor_TrimsAllFields()
    {
        var client = new Client("  Empresa  ", "  Rua A  ", "  123  ", 1);

        client.Name.Should().Be("Empresa");
        client.Address.Should().Be("Rua A");
        client.TaxId.Should().Be("123");
    }

    [Fact(DisplayName = "Client is created with empty address and tax id when not provided")]
    public void Constructor_WithEmptyAddress_CreatesClientWithEmptyAddress()
    {
        var client = new Client("Passageiro", "", "", 1);

        client.Address.Should().BeEmpty();
        client.TaxId.Should().BeEmpty();
    }

    [Fact(DisplayName = "Client address and tax id are normalized to empty string when whitespace-only")]
    public void Constructor_WithWhitespaceAddress_NormalizesToEmpty()
    {
        var client = new Client("Passageiro", "   ", "   ", 1);

        client.Address.Should().BeEmpty();
        client.TaxId.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // Constructor — validations
    // -----------------------------------------------------------------------

    [Theory(DisplayName = "Client creation throws when name is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_Throws(string name)
    {
        var act = () => new Client(name, "", "", 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Client creation throws when name exceeds 200 characters")]
    public void Constructor_WithNameExceeding200Chars_Throws()
    {
        var longName = new string('x', 201);

        var act = () => new Client(longName, "", "", 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Client creation throws when address exceeds 500 characters")]
    public void Constructor_WithAddressExceeding500Chars_Throws()
    {
        var longAddress = new string('x', 501);

        var act = () => new Client("Nome", longAddress, "", 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Client creation throws when tax id exceeds 50 characters")]
    public void Constructor_WithTaxIdExceeding50Chars_Throws()
    {
        var longTaxId = new string('x', 51);

        var act = () => new Client("Nome", "", longTaxId, 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory(DisplayName = "Client creation throws when user id is zero or negative")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidUserId_Throws(int userId)
    {
        var act = () => new Client("Nome", "", "", userId);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("userId");
    }

    // -----------------------------------------------------------------------
    // Update
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Update changes all client fields with valid data")]
    public void Update_WithValidData_UpdatesAllFields()
    {
        var client = new Client("Antigo", "End. Antigo", "000", 1);

        client.Update("Novo", "End. Novo", "111");

        client.Name.Should().Be("Novo");
        client.Address.Should().Be("End. Novo");
        client.TaxId.Should().Be("111");
    }

    [Theory(DisplayName = "Update throws when new name is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithEmptyName_Throws(string name)
    {
        var client = new Client("Nome", "", "", 1);

        var act = () => client.Update(name, "", "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Update clears address and tax id when empty strings are provided")]
    public void Update_WithEmptyAddressAndTaxId_ClearsFields()
    {
        var client = new Client("Nome", "Rua A", "123", 1);

        client.Update("Nome", "", "");

        client.Address.Should().BeEmpty();
        client.TaxId.Should().BeEmpty();
    }
}
