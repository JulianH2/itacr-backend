namespace Itacr.Application.Identity.LoginWithPin;

public sealed record LoginWithPinCommand(Guid EmployeeId, string Pin);
