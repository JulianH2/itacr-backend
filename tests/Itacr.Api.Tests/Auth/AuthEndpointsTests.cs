using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Itacr.Application.Identity;
using Itacr.Application.Identity.Responses;
using Itacr.Domain.Identity;
using Itacr.Domain.Notifications;
using Itacr.Infrastructure.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Itacr.Api.Tests.Auth;

public sealed class AuthEndpointsTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string PinLoginRoute = "/api/v1/auth/login/pin";
    private const string ProblemJsonMediaType = "application/problem+json";
    private const int TokenLifetimeHours = 12;
    private const int MaxFailedPinAttempts = 5;
    private const int LockoutMinutes = 15;

    [Fact]
    public async Task GetEmployees_ShouldReturnOnlyActiveEmployeesOfTheRoleWithoutPrivateData_WhenARoleIsSelected()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var waiter = await factory.AddEmployeeAsync("Ana María Martínez", EmployeeRole.Waiter, "1111");
        await factory.AddEmployeeAsync("Pedro Sánchez", EmployeeRole.Waiter, "2222", EmployeeStatus.PendingApproval);
        await factory.AddEmployeeAsync("Carla Gómez", EmployeeRole.Kitchen, "3333");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/auth/employees?role=Waiter");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = Assert.Single(body.RootElement.EnumerateArray());
        Assert.Equal(["id", "displayName"], item.EnumerateObject().Select(property => property.Name));
        Assert.Equal(waiter.Id, item.GetProperty("id").GetGuid());
        Assert.Equal("Ana M.", item.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task GetEmployees_ShouldReturnValidationProblem_WhenTheRoleIsMissing()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/auth/employees");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithPin_ShouldReturnAShiftTokenWithRoleAndEmployeeId_WhenAnActiveWaiterEntersTheRightPin()
    {
        // Arrange (CA1)
        await using var factory = new AuthApiFactory(Database);
        var waiter = await factory.AddEmployeeAsync("Mesero de Prueba", EmployeeRole.Waiter, "1111");
        using var client = factory.CreateClient();

        // Act
        var login = await factory.LoginAsync(client, waiter.Id, "1111");

        // Assert
        Assert.Equal(new SessionEmployeeResponse(waiter.Id, "Mesero P.", EmployeeRole.Waiter), login.Employee);
        Assert.Equal(factory.Time.GetUtcNow().UtcDateTime.AddHours(TokenLifetimeHours), login.ExpiresAt);
        var token = new JsonWebTokenHandler().ReadJsonWebToken(login.AccessToken);
        Assert.Equal(waiter.Id.ToString(), token.GetClaim(AuthClaimTypes.Subject).Value);
        Assert.Equal(nameof(EmployeeRole.Waiter), token.GetClaim(AuthClaimTypes.Role).Value);
    }

    [Fact]
    public async Task LoginWithPin_ShouldAskForTheFourDigits_WhenThePinHasThreeDigits()
    {
        // Arrange (CA2)
        await using var factory = new AuthApiFactory(Database);
        var waiter = await factory.AddEmployeeAsync("Mesero de Prueba", EmployeeRole.Waiter, "1111");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(PinLoginRoute, new { employeeId = waiter.Id, pin = "111" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(AuthProblems.PinIncomplete, await response.Content.ReadAsStringAsync());
        Assert.Equal(0, await factory.GetFailedLoginAttemptsAsync(waiter.Id));
    }

    [Fact]
    public async Task LoginWithPin_ShouldRejectWithTheInvalidPinMessage_WhenThePinIsWrong()
    {
        // Arrange (CA3)
        await using var factory = new AuthApiFactory(Database);
        var waiter = await factory.AddEmployeeAsync("Mesero de Prueba", EmployeeRole.Waiter, "1111");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(PinLoginRoute, new { employeeId = waiter.Id, pin = "9999" });

        // Assert
        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, AuthProblems.InvalidCredentialsType, AuthProblems.InvalidPin);
        Assert.Equal(1, await factory.GetFailedLoginAttemptsAsync(waiter.Id));
    }

    [Fact]
    public async Task LoginWithPin_ShouldDenyAccessWithThePendingMessage_WhenTheAccountIsPendingApproval()
    {
        // Arrange (CA4)
        await using var factory = new AuthApiFactory(Database);
        var pending = await factory.AddEmployeeAsync("Mesero Pendiente", EmployeeRole.Waiter, "4444", EmployeeStatus.PendingApproval);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(PinLoginRoute, new { employeeId = pending.Id, pin = "4444" });

        // Assert
        await AssertProblemAsync(response, HttpStatusCode.Forbidden, AuthProblems.PendingApprovalType, AuthProblems.PendingApproval);
    }

    [Fact]
    public async Task LoginWithPassword_ShouldDenyAccessWithThePendingMessage_WhenTheAccountIsPendingApproval()
    {
        // Arrange (CA4)
        await using var factory = new AuthApiFactory(Database);
        var pending = await factory.AddEmployeeAsync("Mesero Pendiente", EmployeeRole.Waiter, "4444", EmployeeStatus.PendingApproval);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login/password", new { email = pending.Email, password = AuthApiFactory.Password });

        // Assert
        await AssertProblemAsync(response, HttpStatusCode.Forbidden, AuthProblems.PendingApprovalType, AuthProblems.PendingApproval);
    }

    [Fact]
    public async Task RequestPinReset_ShouldNotifyTheManagerAboutTheEmployee_WhenTheEmployeeForgotThePin()
    {
        // Arrange (CA5)
        await using var factory = new AuthApiFactory(Database);
        var waiter = await factory.AddEmployeeAsync("Mesero de Prueba", EmployeeRole.Waiter, "1111");
        var manager = await factory.AddEmployeeAsync("Gerente de Prueba", EmployeeRole.Manager, "3333");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/auth/pin-reset-requests", new { employeeId = waiter.Id });

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var notification = Assert.Single(await factory.GetNotificationsAsync());
        Assert.Equal(NotificationType.PinReset, notification.Type);
        Assert.Equal(waiter.Id, notification.SubjectEmployeeId);
        Assert.Equal(manager.Id, notification.RecipientEmployeeId);
    }

    [Fact]
    public async Task LoginWithPin_ShouldLockThePinFor15Minutes_WhenTheEmployeeFailsFiveTimes()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var waiter = await factory.AddEmployeeAsync("Mesero de Prueba", EmployeeRole.Waiter, "1111");
        using var client = factory.CreateClient();
        for (var attempt = 1; attempt < MaxFailedPinAttempts; attempt++)
        {
            using var failed = await client.PostAsJsonAsync(PinLoginRoute, new { employeeId = waiter.Id, pin = "9999" });
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        // Act
        using var fifthFailure = await client.PostAsJsonAsync(PinLoginRoute, new { employeeId = waiter.Id, pin = "9999" });
        using var rightPinWhileLocked = await client.PostAsJsonAsync(PinLoginRoute, new { employeeId = waiter.Id, pin = "1111" });
        factory.Time.Advance(TimeSpan.FromMinutes(LockoutMinutes));
        using var rightPinAfterLockout = await client.PostAsJsonAsync(PinLoginRoute, new { employeeId = waiter.Id, pin = "1111" });

        // Assert
        await AssertProblemAsync(fifthFailure, HttpStatusCode.Locked, AuthProblems.LockedType, AuthProblems.Locked);
        await AssertProblemAsync(rightPinWhileLocked, HttpStatusCode.Locked, AuthProblems.LockedType, AuthProblems.Locked);
        Assert.Equal(HttpStatusCode.OK, rightPinAfterLockout.StatusCode);
    }

    [Fact]
    public async Task LoginWithPin_ShouldCountEveryWrongPin_WhenTwoArriveAtTheSameTime()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var waiter = await factory.AddEmployeeAsync("Mesero de Prueba", EmployeeRole.Waiter, "1111");
        using var firstClient = factory.CreateClient();
        using var secondClient = factory.CreateClient();

        // Act
        var responses = await Task.WhenAll(
            firstClient.PostAsJsonAsync(PinLoginRoute, new { employeeId = waiter.Id, pin = "9999" }),
            secondClient.PostAsJsonAsync(PinLoginRoute, new { employeeId = waiter.Id, pin = "8888" }));

        // Assert
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
        Assert.Equal(2, await factory.GetFailedLoginAttemptsAsync(waiter.Id));
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task LoginWithPin_ShouldResetTheFailedCount_WhenTheRightPinFollowsAWrongOne()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var waiter = await factory.AddEmployeeAsync("Mesero de Prueba", EmployeeRole.Waiter, "1111");
        using var client = factory.CreateClient();
        using var wrongPin = await client.PostAsJsonAsync(PinLoginRoute, new { employeeId = waiter.Id, pin = "9999" });

        // Act
        using var rightPin = await client.PostAsJsonAsync(PinLoginRoute, new { employeeId = waiter.Id, pin = "1111" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, rightPin.StatusCode);
        Assert.Equal(0, await factory.GetFailedLoginAttemptsAsync(waiter.Id));
    }

    [Fact]
    public async Task LoginWithPassword_ShouldReturnAToken_WhenAnActiveEmployeeUsesTheRightEmailAndPassword()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var manager = await factory.AddEmployeeAsync("Gerente de Prueba", EmployeeRole.Manager, "3333");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login/password", new { email = manager.Email, password = AuthApiFactory.Password });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(AuthApiFactory.Json);
        Assert.Equal(EmployeeRole.Manager, login?.Employee.Role);
    }

    [Fact]
    public async Task LoginWithPassword_ShouldRejectWithTheInvalidPasswordMessage_WhenThePasswordIsWrong()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var manager = await factory.AddEmployeeAsync("Gerente de Prueba", EmployeeRole.Manager, "3333");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login/password", new { email = manager.Email, password = "wrong-password" });

        // Assert
        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, AuthProblems.InvalidCredentialsType, AuthProblems.InvalidPassword);
    }

    [Fact]
    public async Task GetCurrentEmployee_ShouldReturnTheEmployeeOfTheToken_WhenTheRequestHasAValidToken()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var kitchen = await factory.AddEmployeeAsync("Cocina de Prueba", EmployeeRole.Kitchen, "2222");
        using var client = factory.CreateClient();
        var login = await factory.LoginAsync(client, kitchen.Id, "2222");
        AuthApiFactory.Authorize(client, login.AccessToken);

        // Act
        var employee = await client.GetFromJsonAsync<SessionEmployeeResponse>("/api/v1/auth/me", AuthApiFactory.Json);

        // Assert
        Assert.Equal(new SessionEmployeeResponse(kitchen.Id, "Cocina P.", EmployeeRole.Kitchen), employee);
    }

    [Fact]
    public async Task GetCurrentEmployee_ShouldReturnUnauthorized_WhenTheRequestHasNoToken()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string type, string detail)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(ProblemJsonMediaType, response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(type, problem.RootElement.GetProperty("type").GetString());
        Assert.Equal(detail, problem.RootElement.GetProperty("detail").GetString());
    }
}
