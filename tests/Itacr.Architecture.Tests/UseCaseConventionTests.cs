using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

namespace Itacr.Architecture.Tests;

/// <summary>
/// One folder per use case, with the same module and use case names in Application and the API, and no use case
/// calling another (docs/CODING_STANDARDS.md, section 2.1).
/// </summary>
public sealed class UseCaseConventionTests
{
    private const string HandlerSuffix = "Handler";
    private const string EndpointSuffix = "Endpoint";
    private const string ApplicationRoot = "Itacr.Application.";
    private const string EndpointsRoot = "Itacr.Api.Endpoints.";

    private static readonly Type[] Handlers = [.. Assemblies.Application.GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && type.Name.EndsWith(HandlerSuffix, StringComparison.Ordinal))];

    public static TheoryData<string> HandlerNames => [.. Handlers.Select(handler => handler.FullName!)];

    [Fact]
    public void Application_ShouldHaveUseCases_WhenItIsCompiled() => Assert.NotEmpty(Handlers);

    [Theory]
    [MemberData(nameof(HandlerNames))]
    public void Handler_ShouldLiveInTheFolderOfItsUseCase_WhenItIsCompiled(string handlerName)
    {
        var handler = Handler(handlerName);
        var (_, useCase) = ModuleAndUseCase(handler);

        Assert.Equal($"{useCase}{HandlerSuffix}", handler.Name);
        Assert.True(handler.IsSealed, $"{handler.Name} must be sealed");
    }

    [Theory]
    [MemberData(nameof(HandlerNames))]
    public void Handler_ShouldNotDependOnAnotherHandler_WhenItIsCompiled(string handlerName)
    {
        var handler = Handler(handlerName);
        string[] otherHandlers = [.. Handlers.Where(other => other != handler).Select(other => other.FullName!)];

        Types.InAssembly(Assemblies.Application)
            .That().HaveName(handler.Name).And().ResideInNamespace(handler.Namespace)
            .ShouldNot().HaveDependencyOnAny(otherHandlers)
            .GetResult()
            .AssertSuccessful("A use case must not call another use case; move the shared code to the module root");
    }

    [Theory]
    [MemberData(nameof(HandlerNames))]
    public void Handler_ShouldHaveAnEndpointWithTheSameModuleAndUseCase_WhenItIsCompiled(string handlerName)
    {
        var (module, useCase) = ModuleAndUseCase(Handler(handlerName));
        var endpointName = $"{EndpointsRoot}{module}.{useCase}{EndpointSuffix}";

        Assert.True(Assemblies.Api.GetType(endpointName) is not null, $"Expected the endpoint {endpointName}");
    }

    [Fact]
    public void Endpoints_ShouldBeSealedControllersInTheEndpointsNamespace_WhenTheyAreCompiled()
    {
        Types.InAssembly(Assemblies.Api)
            .That().Inherit(typeof(ControllerBase))
            .Should().ResideInNamespaceStartingWith(EndpointsRoot)
            .And().HaveNameEndingWith(EndpointSuffix)
            .And().BeSealed()
            .GetResult()
            .AssertSuccessful("Controllers are named <UseCase>Endpoint and live in Itacr.Api.Endpoints.<Module>");
    }

    [Fact]
    public void Repositories_ShouldBeInternalSealedClassesInPersistence_WhenTheyAreCompiled()
    {
        Types.InAssembly(Assemblies.Infrastructure)
            .That().HaveNameEndingWith("Repository")
            .Should().BeSealed()
            .And().NotBePublic()
            .And().ResideInNamespaceStartingWith("Itacr.Infrastructure.Persistence")
            .GetResult()
            .AssertSuccessful("Repositories are internal sealed classes in Itacr.Infrastructure.Persistence.<Module>");
    }

    private static Type Handler(string fullName) => Handlers.Single(handler => handler.FullName == fullName);

    private static (string Module, string UseCase) ModuleAndUseCase(Type handler)
    {
        Assert.StartsWith(ApplicationRoot, handler.Namespace);
        var segments = handler.Namespace![ApplicationRoot.Length..].Split('.');
        Assert.True(segments.Length == 2, $"{handler.FullName} must live in Itacr.Application.<Module>.<UseCase>");
        return (segments[0], segments[1]);
    }
}
