using Itacr.Api.Features.Employees;

namespace Itacr.Api.Features.Auth.Responses;

public sealed record SessionEmployeeResponse(Guid Id, string DisplayName, EmployeeRole Role);
