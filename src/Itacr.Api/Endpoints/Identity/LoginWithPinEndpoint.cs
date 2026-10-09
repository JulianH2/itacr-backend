using Itacr.Api.Common;
using Itacr.Api.Common.Http;
using Itacr.Application.Identity;
using Itacr.Application.Identity.LoginWithPin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Itacr.Api.Endpoints.Identity;

[ApiController]
[AllowAnonymous]
[Tags("Auth")]
[Route("api/v1/auth/login/pin")]
public sealed class LoginWithPinEndpoint(LoginWithPinHandler handler) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(AuthServiceCollectionExtensions.LoginRateLimitPolicy)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> HandleAsync(LoginWithPinCommand command, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
