using Itacr.Domain.Identity;

namespace Itacr.Application.Identity.Responses;

public sealed record SessionEmployeeResponse(Guid Id, string DisplayName, EmployeeRole Role);
