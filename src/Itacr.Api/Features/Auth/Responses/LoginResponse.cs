namespace Itacr.Api.Features.Auth.Responses;

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt, SessionEmployeeResponse Employee);
