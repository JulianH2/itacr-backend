using Itacr.Application.Common.Results;

namespace Itacr.Application.Notifications;

public static class NotificationsErrors
{
    public const string SubscriptionInvalid = "La suscripción a notificaciones no es válida. Vuelve a activar los avisos.";

    public static readonly Error PushNotConfigured = new(
        ErrorKind.NotFound,
        "Push not configured",
        "Las notificaciones no están disponibles por ahora.");
}
