using System.ComponentModel.DataAnnotations;

namespace Itacr.Api.Features.Auth.Requests;

public sealed record PasswordLoginRequest(
    [Required(ErrorMessage = AuthProblems.EmailRequired)]
    [EmailAddress(ErrorMessage = AuthProblems.EmailInvalid)]
    [StringLength(254, ErrorMessage = AuthProblems.EmailInvalid)]
    string Email,
    [Required(ErrorMessage = AuthProblems.PasswordRequired)]
    [StringLength(128, ErrorMessage = AuthProblems.InvalidPassword)]
    string Password);
