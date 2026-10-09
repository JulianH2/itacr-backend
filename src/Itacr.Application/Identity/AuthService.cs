using Itacr.Application.Common.Persistence;
using Itacr.Application.Identity.Requests;
using Itacr.Application.Identity.Responses;
using Itacr.Application.Notifications;
using Itacr.Domain.Identity;
using Itacr.Domain.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Itacr.Application.Identity;

public sealed class AuthService(
    IEmployeeRepository employeeRepository,
    INotificationRepository notificationRepository,
    IDbSession dbSession,
    IPasswordHasher<Employee> passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider,
    IOptions<LoginSecurityOptions> loginSecurity,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<IReadOnlyList<EmployeeOptionResponse>> GetActiveEmployeesAsync(EmployeeRole role, CancellationToken cancellationToken)
    {
        var employees = await employeeRepository.GetActiveByRoleAsync(role, cancellationToken);
        return [.. employees.Select(employee => new EmployeeOptionResponse(employee.Id, EmployeeDisplayName.From(employee.FullName)))];
    }

    public async Task<LoginResult> LoginWithPinAsync(PinLoginRequest request, CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return LoginResult.Failed(LoginFailure.InvalidCredentials);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (employee.LockoutEndAt > now)
        {
            return LoginResult.Failed(LoginFailure.Locked);
        }

        var verification = passwordHasher.VerifyHashedPassword(employee, employee.PinHash, request.Pin);
        if (verification == PasswordVerificationResult.Failed)
        {
            return await RegisterFailedPinAsync(employee, now, cancellationToken);
        }

        var newPinHash = verification == PasswordVerificationResult.SuccessRehashNeeded
            ? passwordHasher.HashPassword(employee, request.Pin)
            : null;
        await employeeRepository.RegisterSuccessfulPinLoginAsync(employee.Id, newPinHash, now, cancellationToken);
        return ToLoginResult(employee);
    }

    public async Task<LoginResult> LoginWithPasswordAsync(PasswordLoginRequest request, CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);

        if (employee?.PasswordHash is null
            || passwordHasher.VerifyHashedPassword(employee, employee.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return LoginResult.Failed(LoginFailure.InvalidCredentials);
        }

        return ToLoginResult(employee);
    }

    public async Task<bool> RequestPinResetAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee?.Status != EmployeeStatus.Active)
        {
            return false;
        }

        var managerIds = await employeeRepository.GetActiveManagerIdsAsync(employeeId, cancellationToken);
        IReadOnlyList<Guid?> recipients = managerIds.Count == 0 ? [null] : [.. managerIds.Select(id => (Guid?)id)];

        var sentAt = timeProvider.GetUtcNow().UtcDateTime;
        await dbSession.ExecuteInTransactionAsync(
            async token =>
            {
                foreach (var recipientId in recipients)
                {
                    await notificationRepository.CreateAsync(
                        new Notification
                        {
                            Type = NotificationType.PinReset,
                            SubjectEmployeeId = employeeId,
                            RecipientEmployeeId = recipientId,
                            SentAt = sentAt,
                            CreatedAt = sentAt,
                            UpdatedAt = sentAt,
                        },
                        token);
                }
            },
            cancellationToken);
        return true;
    }

    private async Task<LoginResult> RegisterFailedPinAsync(Employee employee, DateTime now, CancellationToken cancellationToken)
    {
        var settings = loginSecurity.Value;
        var failure = await employeeRepository.RegisterFailedPinLoginAsync(
            employee.Id,
            settings.MaxFailedPinAttempts,
            now.AddMinutes(settings.LockoutMinutes),
            now,
            cancellationToken);

        if (!failure.IsLocked)
        {
            return LoginResult.Failed(LoginFailure.InvalidCredentials);
        }

        logger.LogWarning("PIN login locked for employee {EmployeeId} until {LockoutEndAt}", employee.Id, failure.LockoutEndAt);
        return LoginResult.Failed(LoginFailure.Locked);
    }

    private LoginResult ToLoginResult(Employee employee) => employee.Status switch
    {
        EmployeeStatus.Active => LoginResult.Succeeded(tokenService.CreateLogin(employee)),
        EmployeeStatus.PendingApproval => LoginResult.Failed(LoginFailure.PendingApproval),
        _ => LoginResult.Failed(LoginFailure.Inactive),
    };
}
