using System.Reflection;
using NetArchTest.Rules;

namespace Itacr.Architecture.Tests;

internal static class Assemblies
{
    public static readonly Assembly Domain = typeof(Domain.Common.AuditableEntity).Assembly;

    public static readonly Assembly Application = typeof(Application.DependencyInjection).Assembly;

    public static readonly Assembly Infrastructure = typeof(Infrastructure.DependencyInjection).Assembly;

    public static readonly Assembly Api = typeof(Program).Assembly;

    /// <summary>Fails with the names of the types that broke the rule, so the message says what to fix.</summary>
    public static void AssertSuccessful(this TestResult result, string rule)
    {
        var offenders = result.FailingTypeNames ?? [];
        Assert.True(result.IsSuccessful, $"{rule}. Offending types: {string.Join(", ", offenders)}");
    }
}
