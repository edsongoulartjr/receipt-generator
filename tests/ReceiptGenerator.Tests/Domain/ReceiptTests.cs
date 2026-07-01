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

    [Fact(DisplayName = "ChangeClient accepts null to remove the associated client")]
    public void ChangeClient_WithNull_SetsClientIdToNull()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);

        receipt.ChangeClient(null);

        receipt.ClientId.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // SetNumber
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "SetNumber stores a positive number")]
    public void SetNumber_WithPositiveNumber_StoresNumber()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);

        receipt.SetNumber(42);

        receipt.Number.Should().Be(42);
    }

    [Theory(DisplayName = "SetNumber throws when number is zero or negative")]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetNumber_WithNonPositiveNumber_Throws(int number)
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);

        var act = () => receipt.SetNumber(number);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("number");
    }

    // -----------------------------------------------------------------------
    // Cancel
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Cancel sets IsCancelled to true and records the cancellation time")]
    public void Cancel_WhenNotCancelled_SetsCancelledAtAndIsCancelled()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);
        var before = DateTime.UtcNow;

        receipt.Cancel();

        receipt.IsCancelled.Should().BeTrue();
        receipt.CancelledAt.Should().BeOnOrAfter(before);
    }

    [Fact(DisplayName = "Cancel stores the provided reason trimmed")]
    public void Cancel_WithReason_StoresTrimmedReason()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);

        receipt.Cancel("  Emitido por engano  ");

        receipt.CancelReason.Should().Be("Emitido por engano");
    }

    [Fact(DisplayName = "Cancel sets reason to null when whitespace-only reason is provided")]
    public void Cancel_WithWhitespaceReason_SetsNullReason()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);

        receipt.Cancel("   ");

        receipt.CancelReason.Should().BeNull();
    }

    [Fact(DisplayName = "Cancel is idempotent: calling twice does not update CancelledAt")]
    public void Cancel_WhenAlreadyCancelled_DoesNotUpdateCancelledAt()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);
        receipt.Cancel("primeiro motivo");
        var firstCancelledAt = receipt.CancelledAt;

        receipt.Cancel("segundo motivo");

        receipt.CancelledAt.Should().Be(firstCancelledAt);
        receipt.CancelReason.Should().Be("primeiro motivo");
    }

    // -----------------------------------------------------------------------
    // Update — service date edge cases
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "Update throws when service end date is earlier than service start date")]
    public void Update_WhenServiceEndDateIsBeforeServiceStartDate_Throws()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);
        var start = new DateOnly(2026, 6, 10);
        var end = new DateOnly(2026, 6, 9);

        var act = () => receipt.Update("Corrida", 50m, null, null, null, null, null, null,
            serviceStartDate: start, serviceEndDate: end);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("serviceEndDate");
    }

    [Fact(DisplayName = "Update accepts equal start and end dates for service dates")]
    public void Update_WhenServiceStartDateEqualsServiceEndDate_DoesNotThrow()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);
        var date = new DateOnly(2026, 6, 10);

        var act = () => receipt.Update("Corrida", 50m, null, null, null, null, null, null,
            serviceStartDate: date, serviceEndDate: date);

        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Update accepts equal start and end times")]
    public void Update_WhenStartTimeEqualsEndTime_DoesNotThrow()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);
        var time = DateTime.UtcNow;

        var act = () => receipt.Update("Corrida", 50m, time, time, null, null, null, null);

        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Receipt can be created with null client id")]
    public void Constructor_WithNullClientId_SetsClientIdToNull()
    {
        var receipt = new Receipt(clientId: null, userId: 1, description: "Corrida sem cliente", amount: 50m);

        receipt.ClientId.Should().BeNull();
    }

    [Fact(DisplayName = "Update stores service start and end dates when both are provided")]
    public void Update_WithServiceDates_StoresDates()
    {
        var receipt = new Receipt(1, 1, "Corrida", 50m);
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 1, 31);

        receipt.Update("Corrida", 50m, null, null, null, null, null, null,
            serviceStartDate: start, serviceEndDate: end);

        receipt.ServiceStartDate.Should().Be(start);
        receipt.ServiceEndDate.Should().Be(end);
    }
}
