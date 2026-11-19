namespace Itacr.Application.Manager;

/// <summary>The messages the manager's dashboard answers with, in Spanish because the manager reads them.</summary>
public static class ManagerMessages
{
    public const string FromRequired = "La fecha inicial es obligatoria.";
    public const string ToRequired = "La fecha final es obligatoria.";
    public const string FromAfterTo = "La fecha inicial debe ser anterior a la final.";
    public const string RangeTooLong = "El rango no puede ser mayor a 31 días.";
    public const string PageInvalid = "La página debe estar entre 1 y 100000.";
    public const string PageSizeInvalid = "El tamaño de la página debe estar entre 1 y 100.";
    public const string EmployeeNotFound = "No encontramos a ese empleado.";
    public const string AccountAlreadyHandled = "Esta solicitud ya fue atendida. Actualiza la lista.";
    public const string PinResetAlreadyHandled = "Esta solicitud de PIN ya fue atendida. Actualiza la lista.";
    public const string PinRequired = "Escribe el PIN temporal.";
    public const string PinFourDigits = "El PIN debe tener 4 dígitos.";
}
