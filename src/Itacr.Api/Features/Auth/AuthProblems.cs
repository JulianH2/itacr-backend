namespace Itacr.Api.Features.Auth;

public static class AuthProblems
{
    public const string InvalidCredentialsType = "https://itacr.app/problems/invalid-credentials";
    public const string PendingApprovalType = "https://itacr.app/problems/account-pending-approval";
    public const string InactiveType = "https://itacr.app/problems/account-inactive";
    public const string LockedType = "https://itacr.app/problems/account-locked";

    public const string PinIncomplete = "Ingresa los 4 dígitos de tu PIN.";
    public const string InvalidPin = "PIN incorrecto. Inténtalo de nuevo.";
    public const string InvalidPassword = "Correo o contraseña incorrectos.";
    public const string PendingApproval = "Tu acceso está pendiente de aprobación del gerente.";
    public const string Inactive = "Tu cuenta no está activa. Comunícate con el gerente.";
    public const string Locked = "Demasiados intentos fallidos. Inténtalo de nuevo en 15 minutos.";
    public const string EmailRequired = "Ingresa tu correo.";
    public const string EmailInvalid = "Ingresa un correo válido.";
    public const string PasswordRequired = "Ingresa tu contraseña.";
    public const string EmployeeNotFound = "No encontramos a ese empleado.";
    public const string RoleInvalid = "Selecciona un rol válido.";
}
