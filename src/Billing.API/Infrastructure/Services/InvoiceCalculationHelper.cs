using Billing.API.Domain.Entities;
using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Billing.API.Infrastructure.Services;

internal static class InvoiceCalculationHelper
{
    public sealed record TierSnapshotItem(
        string TierName,
        decimal FromUsage,
        decimal? ToUsage,
        decimal Usage,
        decimal UnitPrice,
        decimal Amount);

    public sealed record CalculationResult(decimal Amount, IReadOnlyList<TierSnapshotItem> Snapshot);

    public static async Task<IReadOnlyList<ServicePriceTier>> GetApplicableTiersAsync(
        BillingDbContext dbContext,
        ServiceType serviceType,
        int roomCapacity,
        DateOnly billingDate,
        CancellationToken cancellationToken)
    {
        var candidateTiers = await dbContext.ServicePriceTiers
            .AsNoTracking()
            .Where(tier => tier.ServiceType == serviceType &&
                           tier.IsActive &&
                           tier.EffectiveFrom <= billingDate &&
                           (tier.EffectiveTo == null || tier.EffectiveTo >= billingDate) &&
                           (tier.RoomCapacity == roomCapacity || tier.RoomCapacity == null))
            .OrderBy(tier => tier.FromUsage)
            .ToListAsync(cancellationToken);

        var exactTiers = candidateTiers
            .Where(tier => tier.RoomCapacity == roomCapacity)
            .OrderBy(tier => tier.FromUsage)
            .ToList();

        var selectedTiers = exactTiers.Count > 0
            ? exactTiers
            : candidateTiers
                .Where(tier => tier.RoomCapacity == null)
                .OrderBy(tier => tier.FromUsage)
                .ToList();

        if (selectedTiers.Count == 0)
        {
            throw new ApiException($"No active {serviceType} price tiers found for room capacity {roomCapacity}.", StatusCodes.Status400BadRequest);
        }

        if (selectedTiers[^1].ToUsage is not null)
        {
            throw new ApiException($"{serviceType} price tiers must include a final open-ended tier.", StatusCodes.Status400BadRequest);
        }

        return selectedTiers;
    }

    public static CalculationResult CalculateTieredAmount(decimal usage, IReadOnlyList<ServicePriceTier> tiers)
    {
        if (usage <= 0)
        {
            return new CalculationResult(0, []);
        }

        var remainingUsage = usage;
        var previousUpperUsage = 0m;
        var amount = 0m;
        var snapshot = new List<TierSnapshotItem>();

        foreach (var tier in tiers)
        {
            if (remainingUsage <= 0)
            {
                break;
            }

            var tierCapacity = tier.ToUsage is null
                ? remainingUsage
                : Math.Max(tier.ToUsage.Value - previousUpperUsage, 0);

            var tierUsage = Math.Min(remainingUsage, tierCapacity);
            if (tierUsage <= 0)
            {
                previousUpperUsage = tier.ToUsage ?? previousUpperUsage;
                continue;
            }

            var tierAmount = tierUsage * tier.UnitPrice;
            amount += tierAmount;
            remainingUsage -= tierUsage;

            snapshot.Add(new TierSnapshotItem(
                tier.TierName,
                tier.FromUsage,
                tier.ToUsage,
                tierUsage,
                tier.UnitPrice,
                tierAmount));

            previousUpperUsage = tier.ToUsage ?? previousUpperUsage + tierUsage;
        }

        if (remainingUsage > 0)
        {
            throw new ApiException("Usage exceeds configured price tiers.", StatusCodes.Status400BadRequest);
        }

        return new CalculationResult(amount, snapshot);
    }
}
