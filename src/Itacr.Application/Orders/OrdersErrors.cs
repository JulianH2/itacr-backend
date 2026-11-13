using Itacr.Application.Common.Results;

namespace Itacr.Application.Orders;

public static class OrdersErrors
{
    public const string DestinationRequired = "Asigna una mesa o marca 'Para llevar' antes de enviar.";
    public const string DestinationAmbiguous = "Elige una mesa o 'Para llevar', no las dos.";
    public const string OrderCannotBeEmpty = "Un pedido no puede quedarse sin productos. Para anularlo, pide ayuda al gerente.";
    public const string NoteTooLong = "La nota no puede tener más de 500 caracteres.";
    public const string TableRequiredMessage = "Un pedido en mesa necesita una mesa.";
    public const string TakeAwayHasNoTableMessage = "Un pedido para llevar no tiene mesa.";
    public const string ItemsRequired = "Agrega al menos un producto al pedido.";
    public const string TooManyItems = "El pedido no puede tener más de 50 productos distintos.";
    public const string QuantityOutOfRange = "La cantidad de cada producto debe estar entre 1 y 99.";
    public const string DuplicatedProduct = "Un producto con las mismas opciones solo puede aparecer una vez; suma la cantidad.";
    public const string SizeRequiredMessage = "Selecciona el tamaño para continuar.";
    public const string DrinkRequiredMessage = "Selecciona la bebida para continuar.";
    public const string NotCustomizableMessage = "Este producto no se puede personalizar.";
    public const string UnknownOptionMessage = "Una de las opciones elegidas ya no está en el menú.";
    public const string DuplicatedOptionMessage = "Cada extra o ingrediente solo se puede elegir una vez.";
    public const string ClientOrderIdRequired = "Falta el identificador del pedido.";
    public const string TableNotFound = "La mesa seleccionada no existe.";
    public const string ProductsNotFound = "Uno o más productos ya no están en el menú.";
    public const string CreatedAtInTheFutureMessage = "La hora del pedido está adelantada. Revisa la hora del dispositivo.";
    public const string CreatedAtTooOldMessage = "El pedido lleva más de 12 horas sin enviarse y ya no se puede mandar a cocina.";

    public const string KitchenStatusOnly = "Cocina solo puede pasar un pedido a 'En preparación' o 'Listo'.";

    public const string OrderConflictType = "https://itacr.app/problems/order-conflict";
    public const string StatusConflictType = "https://itacr.app/problems/order-status-conflict";

    public static readonly Error OrderNotFound = new(ErrorKind.NotFound, "Order not found", "El pedido no existe.");

    public static readonly Error StatusAlreadyChanged = new(
        ErrorKind.Conflict,
        "Order status changed",
        "El pedido ya cambió de estado. Actualiza el tablero.",
        StatusConflictType);

    public static readonly Error OrderNotReady = new(
        ErrorKind.Conflict,
        "Order not ready",
        "El pedido todavía no está listo. Solo se puede entregar cuando cocina lo marca como listo.",
        StatusConflictType);

    public static readonly Error OrderAlreadyDelivered = new(
        ErrorKind.Conflict,
        "Order already delivered",
        "El pedido ya fue entregado.",
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

    public static readonly Error OrderNotModifiable = new(
        ErrorKind.Conflict,
        "Order not modifiable",
        "El pedido ya no se puede modificar porque cocina lo marcó como listo o ya se entregó.",
        StatusConflictType);

    public static readonly Error OrderChangedMeanwhile = new(
        ErrorKind.Conflict,
        "Order changed",
        "El pedido cambió de estado mientras lo modificabas y ya no se puede guardar. Revisa su estado.",
        StatusConflictType);

    public static readonly Error TableRequired = Error.Validation(new Dictionary<string, string[]> { ["tableId"] = [TableRequiredMessage] });

    public static readonly Error TakeAwayHasNoTable = Error.Validation(new Dictionary<string, string[]> { ["tableId"] = [TakeAwayHasNoTableMessage] });

    public static readonly Error ItemsLimitExceeded = Error.Validation(new Dictionary<string, string[]> { ["items"] = [TooManyItems] });

    public static readonly Error UnknownTable = Error.Validation(new Dictionary<string, string[]> { ["tableId"] = [TableNotFound] });

    public static readonly Error UnknownProducts = Error.Validation(new Dictionary<string, string[]> { ["items"] = [ProductsNotFound] });

    public static readonly Error CreatedAtInTheFuture = Error.Validation(new Dictionary<string, string[]> { ["createdAt"] = [CreatedAtInTheFutureMessage] });

    public static readonly Error CreatedAtTooOld = Error.Validation(new Dictionary<string, string[]> { ["createdAt"] = [CreatedAtTooOldMessage] });

    public static readonly Error PlacedByAnotherWaiter = new(
        ErrorKind.Conflict,
        "Order already exists",
        "Este pedido ya fue registrado por otro mesero.",
        OrderConflictType);

    public const string PickupFieldRequired = "Este campo es obligatorio.";
    public const string PickupNameLength = "El nombre debe tener entre 2 y 60 caracteres.";
    public const string PickupNameInvalid = "El nombre tiene caracteres que no se pueden usar.";
    public const string PickupPhoneLength = "El teléfono debe tener 10 dígitos.";
    public const string PickupPhoneInvalid = "Escribe un teléfono válido.";
    public const string PickupLimitType = "https://itacr.app/problems/pickup-order-limit";

    public static Error PickupLimitReached(int maxActiveOrders) => new(
        ErrorKind.Conflict,
        "Pickup order limit reached",
        $"Ya tienes {maxActiveOrders} pedidos en curso. Espera a que los entreguemos para pedir otro.",
        PickupLimitType);

    public static readonly Error PickupOrderIdTaken = new(
        ErrorKind.Conflict,
        "Order already exists",
        "Este pedido ya fue registrado. Arma tu pedido de nuevo.",
        OrderConflictType);
}
