using Itacr.Api.Common.Http;
using Itacr.Application.Identity.GetActiveEmployees;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Itacr.Api.Endpoints.Identity;

[ApiController]
[AllowAnonymous]
[Tags("Auth")]
[Route("api/v1/auth/employees")]
public sealed class GetActiveEmployeesEndpoint(GetActiveEmployeesHandler handler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EmployeeOptionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<EmployeeOptionResponse>>> HandleAsync(
        [FromQuery, BindRequired] EmployeeRole role,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetActiveEmployeesQuery(role), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
