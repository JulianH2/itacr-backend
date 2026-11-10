namespace Itacr.Application.Manager;

/// <summary>The messages the manager's dashboard answers with, in Spanish because the manager reads them.</summary>
public static class ManagerMessages
{
    public const string FromRequired = "La fecha inicial es obligatoria.";
    public const string ToRequired = "La fecha final es obligatoria.";
    public const string FromAfterTo = "La fecha inicial debe ser anterior a la final.";
    public const string RangeTooLong = "El rango no puede ser mayor a 31 días.";
    public const string PageInvalid = "La página debe ser 1 o mayor.";
    public const string PageSizeInvalid = "El tamaño de la página debe estar entre 1 y 100.";
}
