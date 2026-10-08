namespace Itacr.Application.Identity.Responses;

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt, SessionEmployeeResponse Employee);
