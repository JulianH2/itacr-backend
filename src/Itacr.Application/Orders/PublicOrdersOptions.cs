using System.ComponentModel.DataAnnotations;

namespace Itacr.Application.Orders;

/// <summary>
/// Limits of the public endpoints for pickup orders of customers without an account (ITACR-42). They are options, never
/// constants in code, so they can be tuned in QA or production without a release.
/// </summary>
public sealed class PublicOrdersOptions
{
    public const string SectionName = "PublicOrders";

    /// <summary>Active orders (New, InPreparation, Ready) one phone may have.</summary>
    [Range(1, 20)]
    public int MaxActiveOrdersPerPhone { get; init; } = 2;

    /// <summary>Different products in one pickup order; the waiter's limit is higher.</summary>
    [Range(1, 50)]
    public int MaxProducts { get; init; } = 20;

    /// <summary>Units of one product in a pickup order; the waiter's limit is higher.</summary>
    [Range(1, 99)]
    public int MaxQuantity { get; init; } = 10;

    /// <summary>Orders one client IP address may send in <see cref="CreateWindowSeconds"/>.</summary>
    [Range(1, 10_000)]
    public int CreatePermitLimit { get; init; } = 10;

    [Range(1, 86_400)]
    public int CreateWindowSeconds { get; init; } = 600;

    /// <summary>Status queries one client IP address may send in <see cref="StatusWindowSeconds"/>.</summary>
    [Range(1, 10_000)]
    public int StatusPermitLimit { get; init; } = 60;

    [Range(1, 3_600)]
    public int StatusWindowSeconds { get; init; } = 60;
}
