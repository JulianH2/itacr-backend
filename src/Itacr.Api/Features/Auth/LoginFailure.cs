namespace Itacr.Api.Features.Auth;

public enum LoginFailure
{
    InvalidCredentials,
    PendingApproval,
    Inactive,
    Locked,
}
