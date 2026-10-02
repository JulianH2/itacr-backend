using System.Diagnostics;
using System.Security.Claims;
using Itacr.Api.Features.Auth.Requests;
using Itacr.Api.Features.Auth.Responses;
using Itacr.Api.Features.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace Itacr.Api.Features.Auth;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("employees")]
    [ProducesResponseType<IReadOnlyList<EmployeeOptionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<EmployeeOptionResponse>>> GetEmployees(
        [FromQuery, BindRequired] EmployeeRole role,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(role))
        {
            ModelState.AddModelError(nameof(role), AuthProblems.RoleInvalid);
            return ValidationProblem(ModelState);
        }

        return Ok(await authService.GetActiveEmployeesAsync(role, cancellationToken));
    }

    [AllowAnonymous]
    [EnableRateLimiting(AuthServiceCollectionExtensions.LoginRateLimitPolicy)]
    [HttpPost("login/pin")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> LoginWithPin(PinLoginRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await authService.LoginWithPinAsync(request, cancellationToken), AuthProblems.InvalidPin);

    [AllowAnonymous]
    [EnableRateLimiting(AuthServiceCollectionExtensions.LoginRateLimitPolicy)]
    [HttpPost("login/password")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> LoginWithPassword(PasswordLoginRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await authService.LoginWithPasswordAsync(request, cancellationToken), AuthProblems.InvalidPassword);

    [AllowAnonymous]
    [EnableRateLimiting(AuthServiceCollectionExtensions.LoginRateLimitPolicy)]
    [HttpPost("pin-reset-requests")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestPinReset(PinResetRequest request, CancellationToken cancellationToken) =>
        await authService.RequestPinResetAsync(request.EmployeeId, cancellationToken)
            ? Accepted()
            : Problem(AuthProblems.EmployeeNotFound, statusCode: StatusCodes.Status404NotFound, title: "Employee not found");

    [HttpGet("me")]
    [ProducesResponseType<SessionEmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<SessionEmployeeResponse> GetCurrentEmployee()
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

    private ActionResult<LoginResponse> ToActionResult(LoginResult result, string invalidCredentialsDetail) => result.Failure switch
    {
        null when result.Response is not null => Ok(result.Response),
        LoginFailure.InvalidCredentials => LoginProblem(StatusCodes.Status401Unauthorized, "Invalid credentials", AuthProblems.InvalidCredentialsType, invalidCredentialsDetail),
        LoginFailure.PendingApproval => LoginProblem(StatusCodes.Status403Forbidden, "Account pending approval", AuthProblems.PendingApprovalType, AuthProblems.PendingApproval),
        LoginFailure.Inactive => LoginProblem(StatusCodes.Status403Forbidden, "Account not active", AuthProblems.InactiveType, AuthProblems.Inactive),
        LoginFailure.Locked => LoginProblem(StatusCodes.Status423Locked, "Account locked", AuthProblems.LockedType, AuthProblems.Locked),
        _ => throw new UnreachableException($"Unexpected login result {result}"),
    };

    private ObjectResult LoginProblem(int statusCode, string title, string type, string detail) =>
        Problem(detail, statusCode: statusCode, title: title, type: type);
}
