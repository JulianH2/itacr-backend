using FluentValidation;

namespace Itacr.Application.Notifications;

/// <summary>The subscription a browser created with <c>PushManager.subscribe</c>: its endpoint and its two keys (base64url).</summary>
public sealed record PushSubscriptionCommand(string Endpoint, string P256dh, string Auth)
{
    public PushSubscriptionKeys ToKeys() => new(Endpoint, P256dh, Auth);
}

public sealed class PushSubscriptionCommandValidator : AbstractValidator<PushSubscriptionCommand>
{
    private const int MaxEndpointLength = 450;
    private const int MaxKeyLength = 200;
    private const string Base64Url = "^[A-Za-z0-9_-]+=*$";

    public PushSubscriptionCommandValidator()
    {
        RuleFor(command => command.Endpoint)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(NotificationsErrors.SubscriptionInvalid)
            .MaximumLength(MaxEndpointLength).WithMessage(NotificationsErrors.SubscriptionInvalid)
            .Must(endpoint => Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .WithMessage(NotificationsErrors.SubscriptionInvalid);

        RuleFor(command => command.P256dh)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(NotificationsErrors.SubscriptionInvalid)
            .MaximumLength(MaxKeyLength).WithMessage(NotificationsErrors.SubscriptionInvalid)
            .Matches(Base64Url).WithMessage(NotificationsErrors.SubscriptionInvalid);

        RuleFor(command => command.Auth)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(NotificationsErrors.SubscriptionInvalid)
            .MaximumLength(MaxKeyLength).WithMessage(NotificationsErrors.SubscriptionInvalid)
            .Matches(Base64Url).WithMessage(NotificationsErrors.SubscriptionInvalid);
    }
}
