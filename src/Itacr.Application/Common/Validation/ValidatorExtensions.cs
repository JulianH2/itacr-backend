using System.Text.Json;
using FluentValidation;
using Itacr.Application.Common.Results;

namespace Itacr.Application.Common.Validation;

public static class ValidatorExtensions
{
    /// <summary>Validates <paramref name="instance"/> and returns a validation <see cref="Error"/> keyed by camelCase field names, or null when it is valid.</summary>
    public static async Task<Error?> ValidateToErrorAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (result.IsValid)
        {
            return null;
        }

        var fields = result.Errors
            .GroupBy(failure => JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());
        return Error.Validation(fields);
    }
}
