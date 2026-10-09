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

    public const string OrderConflictType = "https://itacr.app/problems/order-conflict";

    public static readonly Error UnknownTable = Error.Validation(new Dictionary<string, string[]> { ["tableId"] = [TableNotFound] });

    public static readonly Error UnknownProducts = Error.Validation(new Dictionary<string, string[]> { ["items"] = [ProductsNotFound] });

    public static readonly Error PlacedByAnotherWaiter = new(
        ErrorKind.Conflict,
        "Order already exists",
        "Este pedido ya fue registrado por otro mesero.",
        OrderConflictType);
}
