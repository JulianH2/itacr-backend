namespace Itacr.Application.Identity;

public enum LoginFailure
{
    InvalidCredentials,
    PendingApproval,
    Inactive,
    Locked,
}
