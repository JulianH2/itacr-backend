using System.ComponentModel.DataAnnotations;

namespace Itacr.Api.Features.Auth.Requests;

public sealed record PinLoginRequest(
    Guid EmployeeId,
    [Required(ErrorMessage = AuthProblems.PinIncomplete)]
    [RegularExpression("^[0-9]{4}$", ErrorMessage = AuthProblems.PinIncomplete)]
    string Pin);
