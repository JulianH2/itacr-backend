using Itacr.Api.Common;
using Itacr.Api.Common.Http;
using Itacr.Application.Identity.RegisterEmployee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Itacr.Api.Endpoints.Identity;

[ApiController]
[AllowAnonymous]
[Tags("Auth")]
[Route("api/v1/auth/registration-requests")]
public sealed class RegisterEmployeeEndpoint(RegisterEmployeeHandler handler) : ControllerBase
{
    /// <summary>A new employee asks for an account from the login screen.</summary>
    /// <remarks>
    /// The account is saved as pending approval, with no password, and nobody can sign in with it until a manager approves it.
    /// <c>email</c> is the email or the user name and must be unique. <c>role</c> is <c>Waiter</c> or <c>Kitchen</c>. The PIN is
    /// 4 digits and <c>pinConfirmation</c> must be the same. <c>acceptedPrivacyNotice</c> must be true; the time is saved.
    /// </remarks>
    /// <param name="command">The data of the new employee.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="202">The request was saved and waits for the manager.</response>
    /// <response code="400">A field is missing or invalid, with the Spanish message of each field.</response>
    /// <response code="409">There is already an account or a request with that email or user.</response>
    /// <response code="429">This address sent too many requests.</response>
    [HttpPost]
    [EnableRateLimiting(RegistrationServiceCollectionExtensions.RateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> HandleAsync(RegisterEmployeeCommand command, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? Accepted() : this.ToActionResult(result.Error!);
    }
}
