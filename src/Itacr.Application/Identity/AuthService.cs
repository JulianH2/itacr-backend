using Itacr.Application.Common.Persistence;
using Itacr.Application.Identity.Requests;
using Itacr.Application.Identity.Responses;
using Itacr.Domain.Identity;
using Itacr.Domain.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Itacr.Application.Identity;

public sealed class AuthService(
    IItacrDbContext dbContext,
    IPasswordHasher<Employee> passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider,
    IOptions<LoginSecurityOptions> loginSecurity,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<IReadOnlyList<EmployeeOptionResponse>> GetActiveEmployeesAsync(EmployeeRole role, CancellationToken cancellationToken)
    {
        var employees = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Role == role && employee.Status == EmployeeStatus.Active)
            .OrderBy(employee => employee.FullName)
            .Select(employee => new { employee.Id, employee.FullName })
            .ToListAsync(cancellationToken);

        return employees
            .Select(employee => new EmployeeOptionResponse(employee.Id, EmployeeDisplayName.From(employee.FullName)))
            .ToList();
    }

    public async Task<LoginResult> LoginWithPinAsync(PinLoginRequest request, CancellationToken cancellationToken)
    {
        var employee = await dbContext.Employees.SingleOrDefaultAsync(candidate => candidate.Id == request.EmployeeId, cancellationToken);
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

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            employee.PinHash = passwordHasher.HashPassword(employee, request.Pin);
        }

        employee.FailedLoginAttempts = 0;
        employee.LockoutEndAt = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToLoginResult(employee);
    }

    public async Task<LoginResult> LoginWithPasswordAsync(PasswordLoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var employee = await dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (employee?.PasswordHash is null
            || passwordHasher.VerifyHashedPassword(employee, employee.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return LoginResult.Failed(LoginFailure.InvalidCredentials);
        }

        return ToLoginResult(employee);
    }

    public async Task<bool> RequestPinResetAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var isActiveEmployee = await dbContext.Employees
            .AnyAsync(employee => employee.Id == employeeId && employee.Status == EmployeeStatus.Active, cancellationToken);
        if (!isActiveEmployee)
        {
            return false;
        }

        var managerIds = await dbContext.Employees
            .Where(employee => employee.Role == EmployeeRole.Manager
                && employee.Status == EmployeeStatus.Active
                && employee.Id != employeeId)
            .Select(employee => (Guid?)employee.Id)
            .ToListAsync(cancellationToken);
        IReadOnlyList<Guid?> recipients = managerIds.Count == 0 ? [null] : managerIds;

        var sentAt = timeProvider.GetUtcNow().UtcDateTime;
        dbContext.Notifications.AddRange(recipients.Select(recipientId => new Notification
        {
            Type = NotificationType.PinReset,
            SubjectEmployeeId = employeeId,
            RecipientEmployeeId = recipientId,
            SentAt = sentAt,
        }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<LoginResult> RegisterFailedPinAsync(Employee employee, DateTime now, CancellationToken cancellationToken)
    {
        var settings = loginSecurity.Value;
        employee.FailedLoginAttempts++;

        if (employee.FailedLoginAttempts < settings.MaxFailedPinAttempts)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return LoginResult.Failed(LoginFailure.InvalidCredentials);
        }

        employee.FailedLoginAttempts = 0;
        employee.LockoutEndAt = now.AddMinutes(settings.LockoutMinutes);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogWarning("PIN login locked for employee {EmployeeId} until {LockoutEndAt}", employee.Id, employee.LockoutEndAt);
        return LoginResult.Failed(LoginFailure.Locked);
    }

    private LoginResult ToLoginResult(Employee employee) => employee.Status switch
    {
        EmployeeStatus.Active => LoginResult.Succeeded(tokenService.CreateLogin(employee)),
        EmployeeStatus.PendingApproval => LoginResult.Failed(LoginFailure.PendingApproval),
        _ => LoginResult.Failed(LoginFailure.Inactive),
    };
}
