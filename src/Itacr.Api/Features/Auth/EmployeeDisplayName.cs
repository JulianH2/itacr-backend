namespace Itacr.Api.Features.Auth;

public static class EmployeeDisplayName
{
    public static string From(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => string.Empty,
            1 => parts[0],
            _ => $"{parts[0]} {char.ToUpperInvariant(parts[^1][0])}.",
        };
    }
}
