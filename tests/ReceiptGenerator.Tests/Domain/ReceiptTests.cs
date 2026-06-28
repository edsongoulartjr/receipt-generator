using AwesomeAssertions;
using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Tests.Domain;

public sealed class ReceiptTests
{
    // -----------------------------------------------------------------------
    // Constructor — happy path
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Receipt is created with all required fields correctly set")]
    public void Constructor_WithValidArguments_CreatesReceipt()
    {
        var receipt = new Receipt(clientId: 1, userId: 2, description: "Transporte executivo", amount: 150.00m);

        receipt.ClientId.Should().Be(1);
        receipt.UserId.Should().Be(2);
        receipt.Description.Should().Be("Transporte executivo");
        receipt.Amount.Should().Be(150.00m);
        receipt.Date.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact(DisplayName = "Receipt description is trimmed of leading and trailing spaces on creation")]
    public void Constructor_TrimsDescription()
    {
        var receipt = new Receipt(1, 1, "  Transporte  ", 50m);

        receipt.Description.Should().Be("Transporte");
    }

    [Fact(DisplayName = "Receipt optional fields are populated when provided")]
    public void Constructor_WithOptionalFields_PopulatesCorrectly()
    {
        var start = DateTime.UtcNow.AddHours(-1);
        var end = DateTime.UtcNow;

        var receipt = new Receipt(1, 1, "Corrida", 80m,
            startTime: start,
            endTime: end,
            serviceDates: "01/06/2026",
            issuerName: "João Silva",
            issuerPhone: "11999999999",
            issuerEmail: "joao@email.com",
            driverName: "Carlos");

        receipt.StartTime.Should().Be(start);
        receipt.EndTime.Should().Be(end);
        receipt.ServiceDates.Should().Be("01/06/2026");
        receipt.IssuerName.Should().Be("João Silva");
        receipt.IssuerPhone.Should().Be("11999999999");
        receipt.IssuerEmail.Should().Be("joao@email.com");
        receipt.DriverName.Should().Be("Carlos");
    }

    // -----------------------------------------------------------------------
    // Constructor — validations
    // -----------------------------------------------------------------------

    [Theory(DisplayName = "Receipt creation throws when client id is zero or negative")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidClientId_Throws(int clientId)
    {
        var act = () => new Receipt(clientId, 1, "Corrida", 50m);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("clientId");
    }

    [Theory(DisplayName = "Receipt creation throws when user id is zero or negative")]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_WithInvalidUserId_Throws(int userId)
    {
        var act = () => new Receipt(1, userId, "Corrida", 50m);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("userId");
    }

    [Theory(DisplayName = "Receipt creation throws when amount is zero or negative")]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void Constructor_WithAmountNotGreaterThanZero_Throws(decimal amount)
    {
        var act = () => new Receipt(1, 1, "Corrida", amount);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory(DisplayName = "Receipt creation throws when description is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyDescription_Throws(string description)
    {
        var act = () => new Receipt(1, 1, description, 50m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Receipt creation throws when description exceeds 1000 characters")]
    public void Constructor_WithDescriptionExceeding1000Chars_Throws()
    {
        var longDescription = new string('x', 1001);

        var act = () => new Receipt(1, 1, longDescription, 50m);

        act.Should().Throw<ArgumentException>();
    }

    // -----------------------------------------------------------------------
    // Update
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Receipt fields are updated with valid data")]
    public void Update_WithValidData_UpdatesFields()
    {
        var receipt = new Receipt(1, 1, "Original", 50m);

        receipt.Update("Atualizado", 200m, null, null, "02/06/2026", null, null, null);

        receipt.Description.Should().Be("Atualizado");
        receipt.Amount.Should().Be(200m);
        receipt.ServiceDates.Should().Be("02/06/2026");
    }

    [Fact(DisplayName = "Update throws when end time is earlier than start time")]
    public void Update_WhenEndTimeIsBeforeStartTime_Throws()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);
        var start = DateTime.UtcNow;
        var end = start.AddMinutes(-30);

        var act = () => receipt.Update("Corrida", 50m, start, end, null, null, null, null);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("endTime");
    }

    [Fact(DisplayName = "Update normalizes optional whitespace-only fields to null")]
    public void Update_OptionalFieldsWithWhitespace_NormalizesToNull()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);

        receipt.Update("Corrida", 50m, null, null, "   ", "  ", null, null);

        receipt.ServiceDates.Should().BeNull();
        receipt.IssuerName.Should().BeNull();
    }

    [Fact(DisplayName = "Update throws when optional field exceeds its maximum length")]
    public void Update_OptionalFieldExceedingMaxLength_Throws()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);
        var longValue = new string('x', 201);

        var act = () => receipt.Update("Corrida", 50m, null, null, null, longValue, null, null);

        act.Should().Throw<ArgumentException>();
    }

    // -----------------------------------------------------------------------
    // ChangeClient
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "ChangeClient updates the client id when a valid id is provided")]
    public void ChangeClient_WithValidId_UpdatesClientId()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);

        receipt.ChangeClient(99);

        receipt.ClientId.Should().Be(99);
    }

    [Theory(DisplayName = "ChangeClient throws when client id is zero or negative")]
    [InlineData(0)]
    [InlineData(-1)]
    public void ChangeClient_WithInvalidId_Throws(int clientId)
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);

        var act = () => receipt.ChangeClient(clientId);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("clientId");
    }
}
