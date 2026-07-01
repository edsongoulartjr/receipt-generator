using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AwesomeAssertions;
using ReceiptGenerator.Application.DTOs;

namespace ReceiptGenerator.Tests.Validation;

/// <summary>
/// Verifica que os atributos de validação (Data Annotations) estão declarados
/// corretamente nos parâmetros dos records posicionais. Em records posicionais
/// do C# os atributos ficam nos parâmetros do construtor primário, e o
/// Validator.TryValidateObject padrão não os enxerga — o ASP.NET Core faz
/// isso internamente via model binding. Por isso os testes inspecionam os
/// metadados via reflection, que é a forma mais fidedigna de validar a
/// intenção da anotação sem mudar o código de produção.
/// </summary>
public sealed class DtoValidationTests
{
    // Retorna os atributos do parâmetro correspondente ao nome da propriedade
    // no construtor primário do record.
    private static IEnumerable<T> GetParamAttributes<T>(Type recordType, string paramName)
        where T : Attribute
    {
        var ctor = recordType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var param = ctor.GetParameters().FirstOrDefault(p =>
            string.Equals(p.Name, paramName, StringComparison.OrdinalIgnoreCase));
        return param?.GetCustomAttributes<T>() ?? Enumerable.Empty<T>();
    }

    private static MaxLengthAttribute? GetMaxLength(Type recordType, string paramName)
        => GetParamAttributes<MaxLengthAttribute>(recordType, paramName).FirstOrDefault();

    private static bool HasRequired(Type recordType, string paramName)
        => GetParamAttributes<RequiredAttribute>(recordType, paramName).Any();

    private static RangeAttribute? GetRange(Type recordType, string paramName)
        => GetParamAttributes<RangeAttribute>(recordType, paramName).FirstOrDefault();

    private static MinLengthAttribute? GetMinLength(Type recordType, string paramName)
        => GetParamAttributes<MinLengthAttribute>(recordType, paramName).FirstOrDefault();

    // -----------------------------------------------------------------------
    // ClientRequest
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "ClientRequest.Name is marked [Required]")]
    public void ClientRequest_Name_IsRequired()
    {
        HasRequired(typeof(ClientRequest), "Name").Should().BeTrue();
    }

    [Fact(DisplayName = "ClientRequest.Name has MaxLength of 200")]
    public void ClientRequest_Name_HasMaxLength200()
    {
        GetMaxLength(typeof(ClientRequest), "Name")!.Length.Should().Be(200);
    }

    [Fact(DisplayName = "ClientRequest.Address has MaxLength of 500")]
    public void ClientRequest_Address_HasMaxLength500()
    {
        GetMaxLength(typeof(ClientRequest), "Address")!.Length.Should().Be(500);
    }

    [Fact(DisplayName = "ClientRequest.TaxId has MaxLength of 50")]
    public void ClientRequest_TaxId_HasMaxLength50()
    {
        GetMaxLength(typeof(ClientRequest), "TaxId")!.Length.Should().Be(50);
    }

    [Fact(DisplayName = "ClientRequest.ZipCode has MaxLength of 9")]
    public void ClientRequest_ZipCode_HasMaxLength9()
    {
        GetMaxLength(typeof(ClientRequest), "ZipCode")!.Length.Should().Be(9);
    }

    [Fact(DisplayName = "ClientRequest.Street has MaxLength of 300")]
    public void ClientRequest_Street_HasMaxLength300()
    {
        GetMaxLength(typeof(ClientRequest), "Street")!.Length.Should().Be(300);
    }

    [Fact(DisplayName = "ClientRequest.Number has MaxLength of 20")]
    public void ClientRequest_Number_HasMaxLength20()
    {
        GetMaxLength(typeof(ClientRequest), "Number")!.Length.Should().Be(20);
    }

    [Fact(DisplayName = "ClientRequest.Complement has MaxLength of 100")]
    public void ClientRequest_Complement_HasMaxLength100()
    {
        GetMaxLength(typeof(ClientRequest), "Complement")!.Length.Should().Be(100);
    }

    [Fact(DisplayName = "ClientRequest.Neighborhood has MaxLength of 100")]
    public void ClientRequest_Neighborhood_HasMaxLength100()
    {
        GetMaxLength(typeof(ClientRequest), "Neighborhood")!.Length.Should().Be(100);
    }

    [Fact(DisplayName = "ClientRequest.City has MaxLength of 100")]
    public void ClientRequest_City_HasMaxLength100()
    {
        GetMaxLength(typeof(ClientRequest), "City")!.Length.Should().Be(100);
    }

    [Fact(DisplayName = "ClientRequest.State has MaxLength of 2")]
    public void ClientRequest_State_HasMaxLength2()
    {
        GetMaxLength(typeof(ClientRequest), "State")!.Length.Should().Be(2);
    }

    // -----------------------------------------------------------------------
    // ReceiptRequest
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "ReceiptRequest.Description is marked [Required]")]
    public void ReceiptRequest_Description_IsRequired()
    {
        HasRequired(typeof(ReceiptRequest), "Description").Should().BeTrue();
    }

    [Fact(DisplayName = "ReceiptRequest.Description has MaxLength of 1000")]
    public void ReceiptRequest_Description_HasMaxLength1000()
    {
        GetMaxLength(typeof(ReceiptRequest), "Description")!.Length.Should().Be(1000);
    }

    [Fact(DisplayName = "ReceiptRequest.Amount has Range with minimum of 0.01")]
    public void ReceiptRequest_Amount_HasRangeMinimum001()
    {
        var range = GetRange(typeof(ReceiptRequest), "Amount");
        range.Should().NotBeNull();
        Convert.ToDouble(range!.Minimum).Should().Be(0.01);
    }

    [Fact(DisplayName = "ReceiptRequest.ServiceDates has MaxLength of 100")]
    public void ReceiptRequest_ServiceDates_HasMaxLength100()
    {
        GetMaxLength(typeof(ReceiptRequest), "ServiceDates")!.Length.Should().Be(100);
    }

    [Fact(DisplayName = "ReceiptRequest.IssuerName has MaxLength of 200")]
    public void ReceiptRequest_IssuerName_HasMaxLength200()
    {
        GetMaxLength(typeof(ReceiptRequest), "IssuerName")!.Length.Should().Be(200);
    }

    [Fact(DisplayName = "ReceiptRequest.IssuerPhone has MaxLength of 50")]
    public void ReceiptRequest_IssuerPhone_HasMaxLength50()
    {
        GetMaxLength(typeof(ReceiptRequest), "IssuerPhone")!.Length.Should().Be(50);
    }

    [Fact(DisplayName = "ReceiptRequest.IssuerEmail has MaxLength of 200")]
    public void ReceiptRequest_IssuerEmail_HasMaxLength200()
    {
        GetMaxLength(typeof(ReceiptRequest), "IssuerEmail")!.Length.Should().Be(200);
    }

    [Fact(DisplayName = "ReceiptRequest.PayerTaxId has MaxLength of 18")]
    public void ReceiptRequest_PayerTaxId_HasMaxLength18()
    {
        GetMaxLength(typeof(ReceiptRequest), "PayerTaxId")!.Length.Should().Be(18);
    }

    // -----------------------------------------------------------------------
    // LoginRequest
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "LoginRequest.Username is marked [Required]")]
    public void LoginRequest_Username_IsRequired()
    {
        HasRequired(typeof(LoginRequest), "Username").Should().BeTrue();
    }

    [Fact(DisplayName = "LoginRequest.Username has MaxLength of 100")]
    public void LoginRequest_Username_HasMaxLength100()
    {
        GetMaxLength(typeof(LoginRequest), "Username")!.Length.Should().Be(100);
    }

    [Fact(DisplayName = "LoginRequest.Password is marked [Required]")]
    public void LoginRequest_Password_IsRequired()
    {
        HasRequired(typeof(LoginRequest), "Password").Should().BeTrue();
    }

    [Fact(DisplayName = "LoginRequest.Password has MaxLength of 100")]
    public void LoginRequest_Password_HasMaxLength100()
    {
        GetMaxLength(typeof(LoginRequest), "Password")!.Length.Should().Be(100);
    }

    // -----------------------------------------------------------------------
    // CreateUserRequest
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "CreateUserRequest.Username is marked [Required]")]
    public void CreateUserRequest_Username_IsRequired()
    {
        HasRequired(typeof(CreateUserRequest), "Username").Should().BeTrue();
    }

    [Fact(DisplayName = "CreateUserRequest.Password is marked [Required]")]
    public void CreateUserRequest_Password_IsRequired()
    {
        HasRequired(typeof(CreateUserRequest), "Password").Should().BeTrue();
    }

    [Fact(DisplayName = "CreateUserRequest.Password has MinLength of 6")]
    public void CreateUserRequest_Password_HasMinLength6()
    {
        GetMinLength(typeof(CreateUserRequest), "Password")!.Length.Should().Be(6);
    }

    [Fact(DisplayName = "CreateUserRequest.Password has MaxLength of 100")]
    public void CreateUserRequest_Password_HasMaxLength100()
    {
        GetMaxLength(typeof(CreateUserRequest), "Password")!.Length.Should().Be(100);
    }

    [Fact(DisplayName = "CreateUserRequest.FullName has MaxLength of 200")]
    public void CreateUserRequest_FullName_HasMaxLength200()
    {
        GetMaxLength(typeof(CreateUserRequest), "FullName")!.Length.Should().Be(200);
    }

    // -----------------------------------------------------------------------
    // Role constant values
    // -----------------------------------------------------------------------

    [Fact(DisplayName = "CreateUserRequest.Role defaults to UserRole.Driver constant value")]
    public void CreateUserRequest_Role_DefaultsToDriverConstant()
    {
        // Verifica que o default do parâmetro Role é o valor correto
        var ctor = typeof(CreateUserRequest).GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length).First();
        var roleParam = ctor.GetParameters().First(p => p.Name == "Role");
        roleParam.DefaultValue.Should().Be("Driver");
    }
}
