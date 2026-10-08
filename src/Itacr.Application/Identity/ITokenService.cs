using Itacr.Application.Identity.Responses;
using Itacr.Domain.Identity;

namespace Itacr.Application.Identity;

public interface ITokenService
{
    LoginResponse CreateLogin(Employee employee);
}
