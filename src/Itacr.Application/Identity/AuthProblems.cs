namespace Itacr.Application.Identity;

public static class AuthProblems
{
    public const string InvalidCredentialsType = "https://itacr.app/problems/invalid-credentials";
    public const string PendingApprovalType = "https://itacr.app/problems/account-pending-approval";
    public const string InactiveType = "https://itacr.app/problems/account-inactive";
    public const string LockedType = "https://itacr.app/problems/account-locked";
    public const string AccountAlreadyExistsType = "https://itacr.app/problems/account-already-exists";

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

    public const string RequiredField = "Este campo es obligatorio.";
    public const string FullNameLength = "El nombre debe tener entre 2 y 120 caracteres.";
    public const string FullNameInvalid = "El nombre tiene caracteres que no se pueden usar.";
    public const string EmailOrUserInvalid = "Escribe un correo o un usuario sin espacios, de 3 a 254 caracteres.";
    public const string PhoneLength = "El teléfono debe tener 10 dígitos.";
    public const string RoleNotRequestable = "Elige mesero o cocina.";
    public const string PinFourDigits = "El PIN debe tener 4 dígitos.";
    public const string PinsDoNotMatch = "Los PIN no coinciden.";
    public const string PrivacyNoticeRequired = "Debes aceptar el aviso de privacidad para continuar.";
    public const string AccountAlreadyExists = "Ya existe una cuenta o una solicitud con ese correo o usuario.";
}
