using Itacr.Api.Features.Auth.Responses;

namespace Itacr.Api.Features.Auth;

public sealed record LoginResult(LoginResponse? Response, LoginFailure? Failure)
{
    public static LoginResult Succeeded(LoginResponse response) => new(response, null);

    public static LoginResult Failed(LoginFailure failure) => new(null, failure);
}
