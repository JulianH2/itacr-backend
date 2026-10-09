using Itacr.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Itacr.Api.Common.Http;

/// <summary>Turns a use case <see cref="Error"/> into the Problem Details response of its HTTP status.</summary>
public static class ErrorActionResults
{
    public const string ProblemJsonMediaType = "application/problem+json";

    public static ActionResult ToActionResult(this ControllerBase controller, Error error)
    {
        if (error.Kind == ErrorKind.Validation)
        {
            var modelState = new ModelStateDictionary();
            foreach (var (field, messages) in error.Fields)
            {
                foreach (var message in messages)
                {
                    modelState.AddModelError(field, message);
                }
            }

            var validation = (ObjectResult)controller.ValidationProblem(modelState);
            validation.ContentTypes.Add(ProblemJsonMediaType);
            return validation;
        }

        return controller.Problem(error.Detail, statusCode: StatusCodeOf(error.Kind), title: error.Title, type: error.Type);
    }

    public static int StatusCodeOf(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Locked => StatusCodes.Status423Locked,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown error kind."),
    };
}
