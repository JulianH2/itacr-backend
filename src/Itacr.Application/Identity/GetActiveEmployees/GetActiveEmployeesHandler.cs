using FluentValidation;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;
using Itacr.Domain.Identity;

namespace Itacr.Application.Identity.GetActiveEmployees;

/// <summary>Lists the active employees of a role for the login picker, with a display name and no private data.</summary>
public sealed class GetActiveEmployeesHandler(IEmployeeRepository employeeRepository, IValidator<GetActiveEmployeesQuery> validator)
{
    public async Task<Result<IReadOnlyList<EmployeeOptionResponse>>> HandleAsync(GetActiveEmployeesQuery query, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(query, cancellationToken) is { } error)
        {
            return error;
        }

        var employees = await employeeRepository.GetActiveByRoleAsync(query.Role, cancellationToken);
        return Result<IReadOnlyList<EmployeeOptionResponse>>.Success(
            [.. employees.Select(employee => new EmployeeOptionResponse(employee.Id, EmployeeDisplayName.From(employee.FullName)))]);
    }
}
