using Itacr.Application.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Itacr.Infrastructure.Notifications;

public static class NotificationsServiceCollectionExtensions
{
    private static readonly TimeSpan PushServiceTimeout = TimeSpan.FromSeconds(10);

    public static IServiceCollection AddPushNotifications(this IServiceCollection services)
    {
        services.AddOptions<WebPushOptions>().BindConfiguration(WebPushOptions.SectionName);
        services.AddSingleton<OrderReadyQueue>();
        services.AddSingleton<IOrderReadyQueue>(serviceProvider => serviceProvider.GetRequiredService<OrderReadyQueue>());
        services.AddHostedService<OrderReadyNotificationService>();
        services.AddHttpClient<IPushSender, WebPushSender>(client => client.Timeout = PushServiceTimeout);
        return services;
    }
}
