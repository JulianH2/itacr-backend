using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Itacr.Api.Common.Http;

/// <summary>
/// The 400 for a request that cannot be bound: malformed JSON, a value of the wrong type (a GUID that is not a GUID),
/// or a missing required parameter. Business validation happens in the use cases; this only covers what never reached
/// them. Field names are camelCase and messages are in Spanish; serializer internals are never returned.
/// </summary>
public static class InvalidRequestResponse
{
    public const string Detail = "La solicitud no tiene un formato válido.";

    private const string JsonRootPath = "$";
    private const string JsonPathPrefix = "$.";

    public static IActionResult Create(ActionContext context)
    {
        var bodyParameters = context.ActionDescriptor.Parameters
            .Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(parameter => parameter.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var errors = new ModelStateDictionary();
        foreach (var (key, entry) in context.ModelState)
        {
            if (entry.ValidationState != ModelValidationState.Invalid || key == JsonRootPath || bodyParameters.Contains(key))
            {
                continue;
            }

            // JSON errors (keys like "$.employeeId") never carry the attempted value: the value was there, but unreadable.
            var field = FieldName(key);
            var isMissing = entry.AttemptedValue is null && !key.StartsWith(JsonPathPrefix, StringComparison.Ordinal);
            errors.AddModelError(field, isMissing ? $"Falta el valor de {field}." : $"El valor de {field} no tiene un formato válido.");
        }

        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(context.HttpContext, errors, StatusCodes.Status400BadRequest, detail: Detail);
        return new BadRequestObjectResult(problem) { ContentTypes = { ErrorActionResults.ProblemJsonMediaType } };
    }

    private static string FieldName(string modelStateKey)
    {
        var name = modelStateKey.StartsWith(JsonPathPrefix, StringComparison.Ordinal) ? modelStateKey[JsonPathPrefix.Length..] : modelStateKey;
        return string.Join('.', name.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
    }
}
