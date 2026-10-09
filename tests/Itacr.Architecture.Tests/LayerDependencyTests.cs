using NetArchTest.Rules;

namespace Itacr.Architecture.Tests;

/// <summary>Clean Architecture: references point inward only (docs/CODING_STANDARDS.md, section 2.1).</summary>
public sealed class LayerDependencyTests
{
    private static readonly string[] DataAccessAndWeb = ["Microsoft.AspNetCore", "Dapper", "Microsoft.Data.SqlClient"];

    [Fact]
    public void Domain_ShouldDependOnNothing_WhenItIsCompiled()
    {
        Types.InAssembly(Assemblies.Domain)
            .ShouldNot().HaveDependencyOnAny(["Itacr.Application", "Itacr.Infrastructure", "Itacr.Api", "FluentValidation", .. DataAccessAndWeb])
            .GetResult()
            .AssertSuccessful("Domain must not depend on another layer, a web framework, or data access");
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructureApiOrFrameworks_WhenItIsCompiled()
    {
        Types.InAssembly(Assemblies.Application)
            .ShouldNot().HaveDependencyOnAny(["Itacr.Infrastructure", "Itacr.Api", .. DataAccessAndWeb])
            .GetResult()
            .AssertSuccessful("Application must not depend on Infrastructure, the API, ASP.NET Core, or data access");
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnTheApi_WhenItIsCompiled()
    {
        Types.InAssembly(Assemblies.Infrastructure)
            .ShouldNot().HaveDependencyOn("Itacr.Api")
            .GetResult()
            .AssertSuccessful("Infrastructure must not depend on the API");
    }

    [Fact]
    public void Api_ShouldReachInfrastructureOnlyFromProgram_WhenItIsCompiled()
    {
        // Program (global namespace) is the composition root; every other API type talks to Application only.
        Types.InAssembly(Assemblies.Api)
            .That().ResideInNamespaceStartingWith("Itacr.Api")
            .ShouldNot().HaveDependencyOn("Itacr.Infrastructure")
            .GetResult()
            .AssertSuccessful("Only Program.cs may use Itacr.Infrastructure in the API");
    }

    [Fact]
    public void Api_ShouldNotUseDataAccess_WhenItIsCompiled()
    {
        Types.InAssembly(Assemblies.Api)
            .ShouldNot().HaveDependencyOnAny("Dapper", "Microsoft.Data.SqlClient")
            .GetResult()
            .AssertSuccessful("The API must not access the database");
    }
}
