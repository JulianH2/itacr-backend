using System.ComponentModel.DataAnnotations;

namespace Itacr.Api.Common.Options;

/// <summary>Origins allowed to call the API from a browser (the frontend dev server and the deployed apps).</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    [Required]
    [MinLength(1)]
    public string[] Origins { get; init; } = [];
}
