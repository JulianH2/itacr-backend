using FluentValidation;

namespace Itacr.Application.Manager;

/// <summary>The rules of the date range both queries of the manager's dashboard take: both ends, in order, and not more than a month.</summary>
internal static class DateRangeRules
{
    public const int MaxDays = 31;

    public static void Apply<T>(AbstractValidator<T> validator, Func<T, DateTimeOffset?> from, Func<T, DateTimeOffset?> to)
    {
        validator.RuleFor(query => from(query)).NotNull().WithMessage(ManagerMessages.FromRequired).OverridePropertyName("from");
        validator.RuleFor(query => to(query)).NotNull().WithMessage(ManagerMessages.ToRequired).OverridePropertyName("to");

        validator.RuleFor(query => query)
            .Must(query => from(query) is not { } start || to(query) is not { } end || start < end)
            .WithMessage(ManagerMessages.FromAfterTo)
            .OverridePropertyName("from");

        validator.RuleFor(query => query)
            .Must(query => from(query) is not { } start || to(query) is not { } end || start >= end || end - start <= TimeSpan.FromDays(MaxDays))
            .WithMessage(ManagerMessages.RangeTooLong)
            .OverridePropertyName("to");
    }
}
