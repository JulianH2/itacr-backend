using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Itacr.Api.Tests.Auth;
using Itacr.Application.Identity;
using Itacr.Application.Manager;
using Itacr.Domain.Identity;
using Microsoft.Data.SqlClient;

namespace Itacr.Api.Tests.Manager;

/// <summary>HU_15: the manager approves or rejects the accounts that asked for access and answers the PIN resets. Runs against the real procedures, as an EXECUTE-only user.</summary>
public sealed class StaffAccessTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string Route = "/api/v1/manager/access-requests";
    private const string PinResetRoute = "/api/v1/manager/pin-reset-requests";
    private const string ChosenPin = "4821";
    private const string TemporaryPin = "9087";

    [Fact]
    public async Task Get_ShouldListThePendingAccountsAndPinResets_WhenThereAreRequests()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (manager, _) = await SignInManagerAsync(factory);
        var pending = await factory.AddEmployeeAsync("Ana López", EmployeeRole.Waiter, ChosenPin, EmployeeStatus.PendingApproval);
        await factory.AddEmployeeAsync("Beto Cocina", EmployeeRole.Kitchen, ChosenPin, EmployeeStatus.Rejected);
        var forgetful = await factory.AddEmployeeAsync("Carla Ruiz", EmployeeRole.Kitchen, ChosenPin);
        await AskPinResetAsync(factory, forgetful.Id);

        // Act
        var requests = await manager.GetFromJsonAsync<JsonElement>(Route);

        // Assert
        var accounts = requests.GetProperty("accounts").EnumerateArray().ToList();
        var account = Assert.Single(accounts);
        Assert.Equal((pending.Id, "Ana López", "Waiter"), (account.GetProperty("id").GetGuid(), account.GetProperty("fullName").GetString(), account.GetProperty("role").GetString()));
        var reset = Assert.Single(requests.GetProperty("pinResets").EnumerateArray());
        Assert.Equal((forgetful.Id, "Carla Ruiz", "Kitchen"), (reset.GetProperty("employeeId").GetGuid(), reset.GetProperty("fullName").GetString(), reset.GetProperty("role").GetString()));
        Assert.DoesNotContain("pinHash", requests.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_ShouldReturnEmptyLists_WhenThereAreNoRequests()
    {
        // Arrange (CA4)
        await using var factory = new AuthApiFactory(Database);
        var (manager, _) = await SignInManagerAsync(factory);

        // Act
        var requests = await manager.GetFromJsonAsync<JsonElement>(Route);

        // Assert
        Assert.Empty(requests.GetProperty("accounts").EnumerateArray());
        Assert.Empty(requests.GetProperty("pinResets").EnumerateArray());
    }

    [Fact]
    public async Task Approve_ShouldActivateTheAccountAndLetTheEmployeeSignIn_WhenTheAccountIsPending()
    {
        // Arrange (CA1)
        await using var factory = new AuthApiFactory(Database);
        var (manager, managerId) = await SignInManagerAsync(factory);
        var pending = await factory.AddEmployeeAsync("Ana López", EmployeeRole.Waiter, ChosenPin, EmployeeStatus.PendingApproval);
        using var anonymous = factory.CreateClient();
        using var beforeApproval = await anonymous.PostAsJsonAsync("/api/v1/auth/login/pin", new { employeeId = pending.Id, pin = ChosenPin });

        // Act
        using var response = await manager.PostAsync($"{Route}/{pending.Id}/approve", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, beforeApproval.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var saved = await ReadEmployeeAsync(pending.Id);
        Assert.Equal(("Active", managerId), (saved.Status, saved.ApprovedById));
        var login = await factory.LoginAsync(anonymous, pending.Id, ChosenPin);
        Assert.Equal(pending.Id, login.Employee.Id);
        Assert.Empty((await manager.GetFromJsonAsync<JsonElement>(Route)).GetProperty("accounts").EnumerateArray());
    }

    [Fact]
    public async Task Reject_ShouldKeepTheAccountRejectedAndStopTheSignIn_WhenTheAccountIsPending()
    {
        // Arrange (CA2)
        await using var factory = new AuthApiFactory(Database);
        var (manager, _) = await SignInManagerAsync(factory);
        var pending = await factory.AddEmployeeAsync("Ana López", EmployeeRole.Waiter, ChosenPin, EmployeeStatus.PendingApproval);

        // Act
        using var response = await manager.PostAsync($"{Route}/{pending.Id}/reject", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var saved = await ReadEmployeeAsync(pending.Id);
        Assert.Equal(("Rejected", (Guid?)null), (saved.Status, saved.ApprovedById));
        using var anonymous = factory.CreateClient();
        using var signIn = await anonymous.PostAsJsonAsync("/api/v1/auth/login/pin", new { employeeId = pending.Id, pin = ChosenPin });
        Assert.Equal(HttpStatusCode.Forbidden, signIn.StatusCode);
        Assert.Contains(AuthProblems.Inactive, await signIn.Content.ReadAsStringAsync());
        using var withPassword = await anonymous.PostAsJsonAsync("/api/v1/auth/login/password", new { email = saved.Email, password = AuthApiFactory.Password });
        Assert.Equal(HttpStatusCode.Forbidden, withPassword.StatusCode);
        Assert.Empty((await manager.GetFromJsonAsync<JsonElement>(Route)).GetProperty("accounts").EnumerateArray());
    }

    [Fact]
    public async Task Reject_ShouldKeepTheEmailTaken_WhenThePersonAsksAgain()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (manager, _) = await SignInManagerAsync(factory);
        using var anonymous = factory.CreateClient();
        using var asked = await anonymous.PostAsJsonAsync("/api/v1/auth/registration-requests", new
        {
            fullName = "Ana López",
            email = "ana.lopez",
            phone = "7711234567",
            role = "Waiter",
            pin = ChosenPin,
            pinConfirmation = ChosenPin,
            acceptedPrivacyNotice = true,
        });
        await using var connection = new SqlConnection(Database.ConnectionString);
        var id = await connection.ExecuteScalarAsync<Guid>("SELECT Id FROM dbo.Employees WHERE Email = 'ana.lopez';");
        using var rejected = await manager.PostAsync($"{Route}/{id}/reject", null);

        // Act
        using var again = await anonymous.PostAsJsonAsync("/api/v1/auth/registration-requests", new
        {
            fullName = "Ana López",
            email = "ana.lopez",
            phone = "7711234567",
            role = "Waiter",
            pin = ChosenPin,
            pinConfirmation = ChosenPin,
            acceptedPrivacyNotice = true,
        });

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, asked.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Theory]
    [InlineData("approve", "approve")]
    [InlineData("approve", "reject")]
    [InlineData("reject", "approve")]
    [InlineData("reject", "reject")]
    public async Task Review_ShouldAnswerConflictAndChangeNothing_WhenTheAccountWasAlreadyHandled(string first, string second)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (manager, managerId) = await SignInManagerAsync(factory);
        var pending = await factory.AddEmployeeAsync("Ana López", EmployeeRole.Waiter, ChosenPin, EmployeeStatus.PendingApproval);
        using var firstResponse = await manager.PostAsync($"{Route}/{pending.Id}/{first}", null);
        var statusAfterFirst = (await ReadEmployeeAsync(pending.Id)).Status;

        // Act
        using var response = await manager.PostAsync($"{Route}/{pending.Id}/{second}", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(ManagerMessages.AccountAlreadyHandled, await response.Content.ReadAsStringAsync());
        Assert.Equal(statusAfterFirst, (await ReadEmployeeAsync(pending.Id)).Status);
        Assert.Equal(first == "approve" ? managerId : null, (await ReadEmployeeAsync(pending.Id)).ApprovedById);
    }

    [Fact]
    public async Task Review_ShouldAnswerConflict_WhenTheEmployeeIsAlreadyActive()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (manager, _) = await SignInManagerAsync(factory);
        var active = await factory.AddEmployeeAsync("Ana López", EmployeeRole.Waiter, ChosenPin);

        // Act
        using var response = await manager.PostAsync($"{Route}/{active.Id}/reject", null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Active", (await ReadEmployeeAsync(active.Id)).Status);
    }

    [Fact]
    public async Task Approve_ShouldSucceedOnlyOnce_WhenTwoManagersApproveAtTheSameTime()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (first, _) = await SignInManagerAsync(factory);
        var (second, _) = await SignInManagerAsync(factory);
        var pending = await factory.AddEmployeeAsync("Ana López", EmployeeRole.Waiter, ChosenPin, EmployeeStatus.PendingApproval);

        // Act
        var responses = await Task.WhenAll(
            first.PostAsync($"{Route}/{pending.Id}/approve", null),
            second.PostAsync($"{Route}/{pending.Id}/approve", null));

        // Assert
        Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.Conflict], responses.Select(response => response.StatusCode).Order());
    }

    [Theory]
    [InlineData("approve")]
    [InlineData("reject")]
    public async Task Review_ShouldAnswerNotFound_WhenTheEmployeeDoesNotExist(string action)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (manager, _) = await SignInManagerAsync(factory);

        // Act
        using var response = await manager.PostAsync($"{Route}/{Guid.NewGuid()}/{action}", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignTemporaryPin_ShouldLetTheEmployeeSignInAndRemoveTheRequest_WhenTheEmployeeAskedToReset()
    {
        // Arrange (CA3): two managers were notified, one of them answers
        await using var factory = new AuthApiFactory(Database);
        var (manager, _) = await SignInManagerAsync(factory);
        await factory.AddEmployeeAsync("Otro Gerente", EmployeeRole.Manager, "5555");
        var waiter = await factory.AddEmployeeAsync("Ana López", EmployeeRole.Waiter, ChosenPin);
        await AskPinResetAsync(factory, waiter.Id);

        // Act
        using var response = await manager.PostAsJsonAsync($"{PinResetRoute}/{waiter.Id}/temporary-pin", new { pin = TemporaryPin });

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var anonymous = factory.CreateClient();
        var login = await factory.LoginAsync(anonymous, waiter.Id, TemporaryPin);
        Assert.Equal(waiter.Id, login.Employee.Id);
        using var oldPin = await anonymous.PostAsJsonAsync("/api/v1/auth/login/pin", new { employeeId = waiter.Id, pin = ChosenPin });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPin.StatusCode);
        Assert.Empty((await manager.GetFromJsonAsync<JsonElement>(Route)).GetProperty("pinResets").EnumerateArray());
        Assert.All(await factory.GetNotificationsAsync(), notification => Assert.NotNull(notification.ResolvedAt));
        Assert.DoesNotContain(TemporaryPin, await factory.GetPinHashAsync(waiter.Id));
    }

    [Fact]
    public async Task AssignTemporaryPin_ShouldClearTheLockout_WhenTheEmployeeWasLocked()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (manager, _) = await SignInManagerAsync(factory);
        var waiter = await factory.AddEmployeeAsync("Ana López", EmployeeRole.Waiter, ChosenPin);
        using var anonymous = factory.CreateClient();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var wrong = await anonymous.PostAsJsonAsync("/api/v1/auth/login/pin", new { employeeId = waiter.Id, pin = "0000" });
        }

        await AskPinResetAsync(factory, waiter.Id);
        using var locked = await anonymous.PostAsJsonAsync("/api/v1/auth/login/pin", new { employeeId = waiter.Id, pin = ChosenPin });

        // Act
        using var response = await manager.PostAsJsonAsync($"{PinResetRoute}/{waiter.Id}/temporary-pin", new { pin = TemporaryPin });

        // Assert
        Assert.Equal(HttpStatusCode.Locked, locked.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await factory.GetFailedLoginAttemptsAsync(waiter.Id));
        Assert.Equal(waiter.Id, (await factory.LoginAsync(anonymous, waiter.Id, TemporaryPin)).Employee.Id);
    }

    [Fact]
    public async Task AssignTemporaryPin_ShouldAnswerConflict_WhenAnotherManagerAlreadyAnswered()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (manager, _) = await SignInManagerAsync(factory);
        var waiter = await factory.AddEmployeeAsync("Ana López", EmployeeRole.Waiter, ChosenPin);
        await AskPinResetAsync(factory, waiter.Id);
        using var first = await manager.PostAsJsonAsync($"{PinResetRoute}/{waiter.Id}/temporary-pin", new { pin = TemporaryPin });

        // Act
        using var response = await manager.PostAsJsonAsync($"{PinResetRoute}/{waiter.Id}/temporary-pin", new { pin = "1357" });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(ManagerMessages.PinResetAlreadyHandled, await response.Content.ReadAsStringAsync());
        using var anonymous = factory.CreateClient();
        Assert.Equal(waiter.Id, (await factory.LoginAsync(anonymous, waiter.Id, TemporaryPin)).Employee.Id);
    }

    [Fact]
    public async Task AssignTemporaryPin_ShouldAnswerConflict_WhenTheEmployeeNeverAskedToReset()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (manager, _) = await SignInManagerAsync(factory);
        var waiter = await factory.AddEmployeeAsync("Ana López", EmployeeRole.Waiter, ChosenPin);

        // Act
        using var response = await manager.PostAsJsonAsync($"{PinResetRoute}/{waiter.Id}/temporary-pin", new { pin = TemporaryPin });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotEqual(string.Empty, await factory.GetPinHashAsync(waiter.Id));
        using var anonymous = factory.CreateClient();
        Assert.Equal(waiter.Id, (await factory.LoginAsync(anonymous, waiter.Id, ChosenPin)).Employee.Id);
    }

    [Fact]
    public async Task AssignTemporaryPin_ShouldAnswerNotFound_WhenTheEmployeeDoesNotExistOrIsNotActive()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (manager, _) = await SignInManagerAsync(factory);
        var pending = await factory.AddEmployeeAsync("Ana López", EmployeeRole.Waiter, ChosenPin, EmployeeStatus.PendingApproval);

        // Act
        using var unknown = await manager.PostAsJsonAsync($"{PinResetRoute}/{Guid.NewGuid()}/temporary-pin", new { pin = TemporaryPin });
        using var notActive = await manager.PostAsJsonAsync($"{PinResetRoute}/{pending.Id}/temporary-pin", new { pin = TemporaryPin });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notActive.StatusCode);
    }

    [Theory]
    [InlineData(null, "Escribe el PIN temporal.")]
    [InlineData("", "Escribe el PIN temporal.")]
    [InlineData("123", "El PIN debe tener 4 dígitos.")]
    [InlineData("12345", "El PIN debe tener 4 dígitos.")]
    [InlineData("12a4", "El PIN debe tener 4 dígitos.")]
    public async Task AssignTemporaryPin_ShouldAskForFourDigits_WhenThePinIsNotValid(string? pin, string message)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (manager, _) = await SignInManagerAsync(factory);
        var waiter = await factory.AddEmployeeAsync("Ana López", EmployeeRole.Waiter, ChosenPin);
        await AskPinResetAsync(factory, waiter.Id);

        // Act
        using var response = await manager.PostAsJsonAsync($"{PinResetRoute}/{waiter.Id}/temporary-pin", new { pin });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal([message], errors.GetProperty("pin").EnumerateArray().Select(error => error.GetString()));
        Assert.Single(await GetPendingPinResetsAsync(manager));
    }

    [Theory]
    [InlineData(EmployeeRole.Waiter)]
    [InlineData(EmployeeRole.Kitchen)]
    public async Task Endpoints_ShouldAnswerForbidden_WhenTheEmployeeIsNotAManager(EmployeeRole role)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var staff = await factory.AddEmployeeAsync("Personal", role, "1234");
        using var client = factory.CreateClient();
        AuthApiFactory.Authorize(client, (await factory.LoginAsync(client, staff.Id, "1234")).AccessToken);
        var pending = await factory.AddEmployeeAsync("Ana López", EmployeeRole.Waiter, ChosenPin, EmployeeStatus.PendingApproval);

        // Act
        var responses = new[]
        {
            await client.GetAsync(Route),
            await client.PostAsync($"{Route}/{pending.Id}/approve", null),
            await client.PostAsync($"{Route}/{pending.Id}/reject", null),
            await client.PostAsJsonAsync($"{PinResetRoute}/{staff.Id}/temporary-pin", new { pin = TemporaryPin }),
        };

        // Assert
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
        Assert.Equal("PendingApproval", (await ReadEmployeeAsync(pending.Id)).Status);
    }

    [Fact]
    public async Task Endpoints_ShouldAnswerUnauthorized_WhenThereIsNoSession()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();

        // Act
        var responses = new[]
        {
            await client.GetAsync(Route),
            await client.PostAsync($"{Route}/{id}/approve", null),
            await client.PostAsync($"{Route}/{id}/reject", null),
            await client.PostAsJsonAsync($"{PinResetRoute}/{id}/temporary-pin", new { pin = TemporaryPin }),
        };

        // Assert
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
    }

    private static async Task<(HttpClient Client, Guid Id)> SignInManagerAsync(AuthApiFactory factory)
    {
        const string Pin = "1234";
        var employee = await factory.AddEmployeeAsync($"Gerente {Guid.NewGuid():N}", EmployeeRole.Manager, Pin);
        var client = factory.CreateClient();
        AuthApiFactory.Authorize(client, (await factory.LoginAsync(client, employee.Id, Pin)).AccessToken);
        return (client, employee.Id);
    }

    private static async Task AskPinResetAsync(AuthApiFactory factory, Guid employeeId)
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/auth/pin-reset-requests", new { employeeId });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<IReadOnlyList<JsonElement>> GetPendingPinResetsAsync(HttpClient manager) =>
        [.. (await manager.GetFromJsonAsync<JsonElement>(Route)).GetProperty("pinResets").EnumerateArray()];

    private async Task<(string Status, Guid? ApprovedById, string Email)> ReadEmployeeAsync(Guid id)
    {
        await using var connection = new SqlConnection(Database.ConnectionString);
        return await connection.QuerySingleAsync<(string Status, Guid? ApprovedById, string Email)>(
            "SELECT Status, ApprovedById, Email FROM dbo.Employees WHERE Id = @Id;",
            new { Id = id });
    }
}
