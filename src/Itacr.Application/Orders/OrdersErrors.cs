using Itacr.Application.Common.Results;

namespace Itacr.Application.Orders;

public static class OrdersErrors
{
    public const string DestinationRequired = "Asigna una mesa o marca 'Para llevar' antes de enviar.";
    public const string DestinationAmbiguous = "Elige una mesa o 'Para llevar', no las dos.";
    public const string ItemsRequired = "Agrega al menos un producto al pedido.";
    public const string TooManyItems = "El pedido no puede tener más de 50 productos distintos.";
    public const string QuantityOutOfRange = "La cantidad de cada producto debe estar entre 1 y 99.";
    public const string DuplicatedProduct = "Cada producto debe aparecer una sola vez en el pedido.";
    public const string ClientOrderIdRequired = "Falta el identificador del pedido.";
    public const string TableNotFound = "La mesa seleccionada no existe.";
    public const string ProductsNotFound = "Uno o más productos ya no están en el menú.";

    public const string KitchenStatusOnly = "Cocina solo puede pasar un pedido a 'En preparación' o 'Listo'.";

    public const string OrderConflictType = "https://itacr.app/problems/order-conflict";
    public const string StatusConflictType = "https://itacr.app/problems/order-status-conflict";

    public static readonly Error OrderNotFound = new(ErrorKind.NotFound, "Order not found", "El pedido no existe.");

    public static readonly Error StatusAlreadyChanged = new(
        ErrorKind.Conflict,
        "Order status changed",
        "El pedido ya cambió de estado. Actualiza el tablero.",
        StatusConflictType);

    public static readonly Error StatusStepNotAllowed = new(
        ErrorKind.Conflict,
        "Order status step not allowed",
        "Un pedido solo puede avanzar al siguiente estado: Nuevo, En preparación, Listo.",
        StatusConflictType);

    public static readonly Error NothingToUndo = new(
        ErrorKind.Conflict,
        "Nothing to undo",
        "Ya no se puede deshacer: pasó el tiempo o el pedido cambió de nuevo.",
        StatusConflictType);

    public static readonly Error UndoByAnotherEmployee = new(
        ErrorKind.Conflict,
        "Undo by another employee",
        "Solo quien hizo el cambio puede deshacerlo.",
        StatusConflictType);

    public static readonly Error UnknownTable = Error.Validation(new Dictionary<string, string[]> { ["tableId"] = [TableNotFound] });

    public static readonly Error UnknownProducts = Error.Validation(new Dictionary<string, string[]> { ["items"] = [ProductsNotFound] });

    public static readonly Error PlacedByAnotherWaiter = new(
        ErrorKind.Conflict,
        "Order already exists",
        "Este pedido ya fue registrado por otro mesero.",
        OrderConflictType);
}
