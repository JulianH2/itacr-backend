namespace Itacr.Application.Common.Results;

/// <summary>The HTTP status family the API answers with; the API keeps the code each endpoint already returns.</summary>
public enum ErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Locked,
}

/// <summary>
/// Why a use case did not succeed. The API turns it into a Problem Details response: <see cref="Detail"/> is shown
/// to the user (Spanish), <see cref="Type"/> is the stable URI clients switch on, and <see cref="Fields"/> holds the
/// validation messages per camelCase field.
/// </summary>
public sealed record Error(ErrorKind Kind, string Title, string Detail, string? Type = null)
{
    public IReadOnlyDictionary<string, string[]> Fields { get; init; } = new Dictionary<string, string[]>();

    public static Error Validation(IReadOnlyDictionary<string, string[]> fields) =>
        new(ErrorKind.Validation, "One or more validation errors occurred.", "Revisa los datos enviados.") { Fields = fields };
}
