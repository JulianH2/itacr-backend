using Itacr.Api.Features.Auth.Responses;
using Itacr.Api.Features.Employees;

namespace Itacr.Api.Features.Auth;

public interface ITokenService
{
    LoginResponse CreateLogin(Employee employee);
}
