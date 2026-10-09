using System.Net;
using System.Text;
using System.Text.Json;
using Itacr.Api.Common.Http;
using Microsoft.AspNetCore.Http;

namespace Itacr.Api.Tests;

/// <summary>
/// Requests that never reach a use case, and validation messages: always a 400 Problem Details (never a 500), with
/// camelCase field names and Spanish messages. None of these requests opens a database connection.
/// </summary>
public sealed class InvalidRequestTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string ProblemJsonMediaType = "application/problem+json";

    [Theory]
    [InlineData("/api/v1/auth/login/pin", "{\"employeeId\": ")]
    [InlineData("/api/v1/auth/login/pin", "not json")]
    [InlineData("/api/v1/auth/pin-reset-requests", "{")]
    public async Task Post_ShouldReturnASpanishProblem_WhenTheJsonIsMalformed(string route, string body)
    {
        // Act
        using var response = await PostJsonAsync(route, body);

        // Assert
        var problem = await AssertBadRequestAsync(response);
        Assert.Equal(InvalidRequestResponse.Detail, problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task LoginWithPin_ShouldNameTheField_WhenTheEmployeeIdIsNotAGuid()
    {
        // Act
        using var response = await PostJsonAsync("/api/v1/auth/login/pin", """{"employeeId":"abc","pin":"1234"}""");

        // Assert
        var problem = await AssertBadRequestAsync(response);
        Assert.Equal(["El valor de employeeId no tiene un formato válido."], Messages(problem, "employeeId"));
    }

    [Fact]
    public async Task LoginWithPin_ShouldReturnTheCamelCaseField_WhenThePinHasThreeDigits()
    {
        // Act
        using var response = await PostJsonAsync("/api/v1/auth/login/pin", $$"""{"employeeId":"{{Guid.NewGuid()}}","pin":"123"}""");

        // Assert
        var problem = await AssertBadRequestAsync(response);
        Assert.Equal(["pin"], FieldNames(problem));
        Assert.Equal(["Ingresa los 4 dígitos de tu PIN."], Messages(problem, "pin"));
    }

    [Fact]
    public async Task LoginWithPassword_ShouldReturnBothCamelCaseFields_WhenEmailAndPasswordAreEmpty()
    {
        // Act
        using var response = await PostJsonAsync("/api/v1/auth/login/password", """{"email":"","password":""}""");

        // Assert
        var problem = await AssertBadRequestAsync(response);
        Assert.Equal(["email", "password"], FieldNames(problem));
        Assert.Equal(["Ingresa tu correo."], Messages(problem, "email"));
        Assert.Equal(["Ingresa tu contraseña."], Messages(problem, "password"));
    }

    [Theory]
    [InlineData("/api/v1/auth/employees", "Falta el valor de role.")]
    [InlineData("/api/v1/auth/employees?role=Chef", "El valor de role no tiene un formato válido.")]
    [InlineData("/api/v1/auth/employees?role=99", "El valor de role no tiene un formato válido.")]
    public async Task GetActiveEmployees_ShouldExplainTheRole_WhenItIsMissingOrUnknown(string url, string message)
    {
        // Act
        using var response = await factory.CreateClient().GetAsync(url);

        // Assert
        var problem = await AssertBadRequestAsync(response);
        Assert.Equal([message], Messages(problem, "role"));
    }

    [Theory]
    [InlineData("/api/v1/catalog/products", "El parámetro categoryId es obligatorio.")]
    [InlineData("/api/v1/catalog/products?categoryId=not-a-guid", "El valor de categoryId no tiene un formato válido.")]
    public async Task GetProducts_ShouldExplainTheCategoryId_WhenItIsMissingOrNotAGuid(string url, string message)
    {
        // Act
        using var response = await factory.CreateClient().GetAsync(url);

        // Assert
        var problem = await AssertBadRequestAsync(response);
        Assert.Equal([message], Messages(problem, "categoryId"));
    }

    private async Task<HttpResponseMessage> PostJsonAsync(string route, string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        return await factory.CreateClient().PostAsync(route, content);
    }

    private static async Task<JsonElement> AssertBadRequestAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJsonMediaType, response.Content.Headers.ContentType?.MediaType);
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
        Assert.Equal(StatusCodes.Status400BadRequest, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("traceId", out _));
        return problem;
    }

    private static string[] FieldNames(JsonElement problem) =>
        [.. problem.GetProperty("errors").EnumerateObject().Select(field => field.Name).Order(StringComparer.Ordinal)];

    private static string[] Messages(JsonElement problem, string field) =>
        [.. problem.GetProperty("errors").GetProperty(field).EnumerateArray().Select(message => message.GetString() ?? string.Empty)];
}
