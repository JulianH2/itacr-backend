using Itacr.Domain.Identity;

namespace Itacr.Application.Identity;

public sealed record SessionEmployeeResponse(Guid Id, string DisplayName, EmployeeRole Role);
