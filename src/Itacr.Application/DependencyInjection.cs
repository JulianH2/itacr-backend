using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Itacr.Application;

public static class DependencyInjection
{
    private const string HandlerSuffix = "Handler";

    /// <summary>Registers every use case handler and validator of this assembly, so adding a use case never edits this file.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        foreach (var handler in assembly.GetTypes().Where(type => type is { IsClass: true, IsAbstract: false } && type.Name.EndsWith(HandlerSuffix, StringComparison.Ordinal)))
        {
            services.AddScoped(handler);
        }

        services.AddValidatorsFromAssembly(assembly);
        return services;
    }
}
