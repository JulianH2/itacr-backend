using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Itacr.Application.Identity;
using Itacr.Domain.Identity;
using Microsoft.Data.SqlClient;

namespace Itacr.Api.Tests.Auth;

/// <summary>HU_14: a new employee asks for an account from the login screen. Runs against the real procedures, as an EXECUTE-only user.</summary>
public sealed class RegistrationTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string Route = "/api/v1/auth/registration-requests";

    [Fact]
    public async Task Post_ShouldSaveAPendingAccountWithoutLogin_WhenAllTheFieldsAreCorrect()
    {
        // Arrange (CA1)
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(Route, Body());

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await using var connection = new SqlConnection(Database.ConnectionString);
        var saved = await connection.QuerySingleAsync<(string FullName, string Email, string Phone, string Role, string Status, string PinHash, string? PasswordHash, Guid? ApprovedById, DateTime? PrivacyNoticeAcceptedAt)>(
            "SELECT FullName, Email, Phone, Role, Status, PinHash, PasswordHash, ApprovedById, PrivacyNoticeAcceptedAt FROM dbo.Employees WHERE Email = 'ana.lopez';");
        Assert.Equal(("Ana López", "ana.lopez", "7711234567", "Waiter", "PendingApproval"), (saved.FullName, saved.Email, saved.Phone, saved.Role, saved.Status));
        Assert.Null(saved.PasswordHash);
        Assert.Null(saved.ApprovedById);
        Assert.NotNull(saved.PrivacyNoticeAcceptedAt);
        Assert.DoesNotContain("4821", saved.PinHash);
        Assert.NotEmpty(saved.PinHash);
    }

    [Fact]
    public async Task Post_ShouldTrimTheFields_WhenTheyCarrySpaces()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(Route, Body(fullName: "  Ana López  ", email: " ana.lopez ", phone: " 7711234567 "));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await using var connection = new SqlConnection(Database.ConnectionString);
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Employees WHERE Email = 'ana.lopez' AND FullName = N'Ana López';"));
    }

    [Fact]
    public async Task Post_ShouldNotLetTheAccountSignIn_WhenItIsStillPending()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync(Route, Body());
        await using var connection = new SqlConnection(Database.ConnectionString);
        var id = await connection.ExecuteScalarAsync<Guid>("SELECT Id FROM dbo.Employees WHERE Email = 'ana.lopez';");

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login/pin", new { employeeId = id, pin = "4821" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(AuthProblems.PendingApproval, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_ShouldAskForEveryField_WhenTheBodyIsEmpty()
    {
        // Arrange (CA2)
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(Route, new { });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        foreach (var field in new[] { "fullName", "email", "phone", "role", "pin", "pinConfirmation" })
        {
            Assert.Equal([AuthProblems.RequiredField], errors.GetProperty(field).EnumerateArray().Select(message => message.GetString()));
        }

        Assert.Equal([AuthProblems.PrivacyNoticeRequired], errors.GetProperty("acceptedPrivacyNotice").EnumerateArray().Select(message => message.GetString()));
        Assert.Equal(0, await CountEmployeesAsync("Email IS NOT NULL AND Status = 'PendingApproval'"));
    }

    [Theory]
    [InlineData("fullName", "   ")]
    [InlineData("email", "")]
    [InlineData("phone", "  ")]
    public async Task Post_ShouldAskForTheField_WhenARequiredTextIsBlank(string field, string value)
    {
        // Arrange (CA2)
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        var body = ToDictionary(Body());
        body[field] = value;

        // Act
        using var response = await client.PostAsJsonAsync(Route, body);

        // Assert
        Assert.Equal([AuthProblems.RequiredField], await FieldMessagesAsync(response, field));
    }

    [Fact]
    public async Task Post_ShouldSayThePinsDoNotMatch_WhenTheConfirmationIsDifferent()
    {
        // Arrange (CA3)
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(Route, Body(pin: "4821", pinConfirmation: "4822"));

        // Assert
        Assert.Equal(["Los PIN no coinciden."], await FieldMessagesAsync(response, "pinConfirmation"));
        Assert.Equal(0, await CountEmployeesAsync("Email = 'ana.lopez'"));
    }

    [Fact]
    public async Task Post_ShouldRejectTheRequest_WhenThePrivacyNoticeIsNotAccepted()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(Route, Body(acceptedPrivacyNotice: false));

        // Assert
        Assert.Equal([AuthProblems.PrivacyNoticeRequired], await FieldMessagesAsync(response, "acceptedPrivacyNotice"));
        Assert.Equal(0, await CountEmployeesAsync("Email = 'ana.lopez'"));
    }

    [Theory]
    [InlineData("pin", "123", "El PIN debe tener 4 dígitos.")]
    [InlineData("pin", "12a4", "El PIN debe tener 4 dígitos.")]
    [InlineData("phone", "771123", "El teléfono debe tener 10 dígitos.")]
    [InlineData("phone", "77112345ab", "El teléfono debe tener 10 dígitos.")]
    [InlineData("email", "ana lopez", "Escribe un correo o un usuario sin espacios, de 3 a 254 caracteres.")]
    [InlineData("email", "ab", "Escribe un correo o un usuario sin espacios, de 3 a 254 caracteres.")]
    [InlineData("fullName", "A", "El nombre debe tener entre 2 y 120 caracteres.")]
    public async Task Post_ShouldRejectTheRequest_WhenAFieldIsNotValid(string field, string value, string message)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        var body = ToDictionary(Body());
        body[field] = value;
        if (field == "pin")
        {
            body["pinConfirmation"] = value;
        }

        // Act
        using var response = await client.PostAsJsonAsync(Route, body);

        // Assert
        Assert.Equal([message], await FieldMessagesAsync(response, field));
    }

    [Theory]
    [InlineData("Manager")]
    [InlineData("Admin")]
    public async Task Post_ShouldRejectTheRequest_WhenTheRoleCannotBeRequested(string role)
    {
        // Arrange: the manager role is never requested from the public form
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        var body = ToDictionary(Body());
        body["role"] = role;

        // Act
        using var response = await client.PostAsJsonAsync(Route, body);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountEmployeesAsync("Email = 'ana.lopez'"));
    }

    [Fact]
    public async Task Post_ShouldAnswerConflictAndSaveNothing_WhenTheEmailOrUserIsTaken()
    {
        // Arrange: another request with the same user, in another case
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        using var first = await client.PostAsJsonAsync(Route, Body(email: "ana.lopez"));

        // Act
        using var second = await client.PostAsJsonAsync(Route, Body(fullName: "Otra Persona", email: "ANA.LOPEZ", role: "Kitchen"));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(AuthProblems.AccountAlreadyExists, problem.GetProperty("detail").GetString());
        await using var connection = new SqlConnection(Database.ConnectionString);
        Assert.Equal(["Ana López"], await connection.QueryAsync<string>("SELECT FullName FROM dbo.Employees WHERE Email = 'ana.lopez';"));
    }

    [Fact]
    public async Task Post_ShouldAnswerConflict_WhenTheEmailBelongsToAnActiveEmployee()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var employee = await factory.AddEmployeeAsync("Mesero de Prueba", EmployeeRole.Waiter, "1111");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(Route, Body(email: employee.Email));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_ShouldAnswerTooManyRequests_WhenOneAddressSendsMoreRequestsThanItsLimit()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database, new Dictionary<string, string> { ["Registration:PermitLimit"] = "2" });
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync(Route, Body(email: "uno"));
        await client.PostAsJsonAsync(Route, Body(email: "dos"));

        // Act
        using var third = await client.PostAsJsonAsync(Route, Body(email: "tres"));

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Equal(0, await CountEmployeesAsync("Email = 'tres'"));
    }

    private static Dictionary<string, object?> Body(
        string fullName = "Ana López",
        string email = "ana.lopez",
        string phone = "7711234567",
        string role = "Waiter",
        string pin = "4821",
        string? pinConfirmation = null,
        bool acceptedPrivacyNotice = true) => new()
        {
            ["fullName"] = fullName,
            ["email"] = email,
            ["phone"] = phone,
            ["role"] = role,
            ["pin"] = pin,
            ["pinConfirmation"] = pinConfirmation ?? pin,
            ["acceptedPrivacyNotice"] = acceptedPrivacyNotice,
        };

    private static Dictionary<string, object?> ToDictionary(Dictionary<string, object?> body) => new(body);

    private static async Task<string[]> FieldMessagesAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return [.. problem.GetProperty("errors").GetProperty(field).EnumerateArray().Select(message => message.GetString() ?? string.Empty)];
    }

    private async Task<int> CountEmployeesAsync(string where)
    {
        await using var connection = new SqlConnection(Database.ConnectionString);
        return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM dbo.Employees WHERE {where};");
    }
}
