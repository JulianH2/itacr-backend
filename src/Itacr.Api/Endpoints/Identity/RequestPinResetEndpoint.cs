using Itacr.Api.Common;
using Itacr.Api.Common.Http;
using Itacr.Application.Identity.RequestPinReset;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Itacr.Api.Endpoints.Identity;

[ApiController]
[AllowAnonymous]
[Tags("Auth")]
[Route("api/v1/auth/pin-reset-requests")]
public sealed class RequestPinResetEndpoint(RequestPinResetHandler handler) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(AuthServiceCollectionExtensions.LoginRateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> HandleAsync(RequestPinResetCommand command, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? Accepted() : this.ToActionResult(result.Error!);
    }
}
