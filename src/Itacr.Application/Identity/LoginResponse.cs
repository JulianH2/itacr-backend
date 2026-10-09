namespace Itacr.Application.Identity;

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt, SessionEmployeeResponse Employee);
