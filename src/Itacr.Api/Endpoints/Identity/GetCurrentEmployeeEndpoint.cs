using System.Security.Claims;
using Itacr.Application.Identity;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Identity;

/// <summary>Reads the session from the token claims only; there is no use case behind it.</summary>
[ApiController]
[Tags("Auth")]
[Route("api/v1/auth/me")]
public sealed class GetCurrentEmployeeEndpoint : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<SessionEmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<SessionEmployeeResponse> Handle()
    {
        var displayName = User.FindFirstValue(AuthClaimTypes.Name);
        if (displayName is null
            || !Guid.TryParse(User.FindFirstValue(AuthClaimTypes.Subject), out var employeeId)
            || !Enum.TryParse<EmployeeRole>(User.FindFirstValue(AuthClaimTypes.Role), out var role))
        {
            return Unauthorized();
        }

        return Ok(new SessionEmployeeResponse(employeeId, displayName, role));
    }
}
