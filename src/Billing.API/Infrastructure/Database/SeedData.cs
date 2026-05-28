using Billing.API.Domain.Entities;
using Billing.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Infrastructure.Database;

public static class SeedData
{
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    public static async Task SeedAsync(BillingDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await SeedServicePriceTiersAsync(dbContext, cancellationToken);
        await SeedContractTemplatesAsync(dbContext, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedServicePriceTiersAsync(BillingDbContext dbContext, CancellationToken cancellationToken)
    {
        var seedTiers = GetServicePriceTiers();
        var existingKeys = await dbContext.ServicePriceTiers
            .Select(t => new TierKey(t.ServiceType, t.RoomCapacity, t.EffectiveFrom, t.FromUsage))
            .ToListAsync(cancellationToken);

        var existingKeySet = existingKeys.ToHashSet();
        var missingTiers = seedTiers
            .Where(t => !existingKeySet.Contains(new TierKey(t.ServiceType, t.RoomCapacity, t.EffectiveFrom, t.FromUsage)))
            .ToList();

        if (missingTiers.Count > 0)
        {
            await dbContext.ServicePriceTiers.AddRangeAsync(missingTiers, cancellationToken);
        }
    }

    private static async Task SeedContractTemplatesAsync(BillingDbContext dbContext, CancellationToken cancellationToken)
    {
        var templates = GetContractTemplates();
        var existingKeys = await dbContext.ContractTemplates
            .Select(t => new ContractTemplateKey(t.Code, t.Version))
            .ToListAsync(cancellationToken);

        var existingKeySet = existingKeys.ToHashSet();
        var missingTemplates = templates
            .Where(t => !existingKeySet.Contains(new ContractTemplateKey(t.Code, t.Version)))
            .ToList();

        if (missingTemplates.Count > 0)
        {
            await dbContext.ContractTemplates.AddRangeAsync(missingTemplates, cancellationToken);
        }
    }

    private static IReadOnlyList<ServicePriceTier> GetServicePriceTiers()
    {
        List<ServicePriceTier> tiers = [];

        AddElectricityTiers(tiers, 8, [(0m, 100m), (101m, 200m), (201m, 400m), (401m, 600m), (601m, 800m), (801m, null)]);
        AddElectricityTiers(tiers, 7, [(0m, 87.5m), (87.6m, 175m), (176m, 350m), (351m, 525m), (526m, 700m), (701m, null)]);
        AddElectricityTiers(tiers, 6, [(0m, 75m), (76m, 150m), (151m, 300m), (301m, 450m), (451m, 600m), (601m, null)]);
        AddElectricityTiers(tiers, 5, [(0m, 62.5m), (62.6m, 125m), (126m, 250m), (251m, 375m), (376m, 500m), (501m, null)]);
        AddElectricityTiers(tiers, 4, [(0m, 50m), (51m, 100m), (101m, 200m), (201m, 300m), (301m, 400m), (401m, null)]);
        AddElectricityTiers(tiers, 3, [(0m, 37.5m), (37.6m, 75m), (76m, 150m), (151m, 225m), (226m, 300m), (301m, null)]);
        AddElectricityTiers(tiers, 2, [(0m, 25m), (26m, 50m), (51m, 100m), (101m, 150m), (151m, 200m), (201m, null)]);
        AddElectricityTiers(tiers, 1, [(0m, 12.5m), (12.6m, 25m), (26m, 50m), (51m, 75m), (76m, 100m), (101m, null)]);

        AddWaterTiers(tiers, 8, [(0m, 32m), (32.1m, 48m), (48.1m, null)]);
        AddWaterTiers(tiers, 7, [(0m, 28m), (28.1m, 42m), (42.1m, null)]);
        AddWaterTiers(tiers, 6, [(0m, 24m), (24.1m, 36m), (36.1m, null)]);
        AddWaterTiers(tiers, 5, [(0m, 20m), (20.1m, 30m), (30.1m, null)]);
        AddWaterTiers(tiers, 4, [(0m, 16m), (16.1m, 24m), (24.1m, null)]);
        AddWaterTiers(tiers, 3, [(0m, 12m), (12.1m, 18m), (18.1m, null)]);
        AddWaterTiers(tiers, 2, [(0m, 8m), (8.1m, 12m), (12.1m, null)]);
        AddWaterTiers(tiers, 1, [(0m, 4m), (4.1m, 6m), (6.1m, null)]);

        return tiers;
    }

    private static void AddElectricityTiers(List<ServicePriceTier> tiers, int roomCapacity, IReadOnlyList<(decimal From, decimal? To)> ranges)
    {
        decimal[] prices = [1984m, 2050m, 2380m, 2998m, 3350m, 3460m];

        for (var i = 0; i < ranges.Count; i++)
        {
            tiers.Add(CreateTier(ServiceType.Electricity, roomCapacity, i + 1, ranges[i].From, ranges[i].To, "kWh", prices[i]));
        }
    }

    private static void AddWaterTiers(List<ServicePriceTier> tiers, int roomCapacity, IReadOnlyList<(decimal From, decimal? To)> ranges)
    {
        decimal[] prices = [5485m, 10557m, 11799m];

        for (var i = 0; i < ranges.Count; i++)
        {
            tiers.Add(CreateTier(ServiceType.Water, roomCapacity, i + 1, ranges[i].From, ranges[i].To, "m3", prices[i]));
        }
    }

    private static ServicePriceTier CreateTier(
        ServiceType serviceType,
        int roomCapacity,
        int tierNumber,
        decimal fromUsage,
        decimal? toUsage,
        string unitName,
        decimal unitPrice)
    {
        var now = DateTime.UtcNow;

        return new ServicePriceTier
        {
            ServiceType = serviceType,
            RoomCapacity = roomCapacity,
            TierName = $"Bac {tierNumber}",
            FromUsage = fromUsage,
            ToUsage = toUsage,
            UnitName = unitName,
            UnitPrice = unitPrice,
            EffectiveFrom = EffectiveFrom,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static IReadOnlyList<ContractTemplate> GetContractTemplates()
    {
        var now = DateTime.UtcNow;

        return
        [
            new ContractTemplate
            {
                Code = "STANDARD_DORM_CONTRACT",
                Name = "Standard Dormitory Contract",
                Version = 1,
                Content = """
                          # Standard Dormitory Contract

                          This template defines the baseline dormitory occupancy terms, monthly utility billing responsibilities, payment obligations, and resident conduct rules.

                          Utility invoices are calculated from room meter indices and the active service price tiers at the time of billing. Electricity prices exclude VAT; water prices already include VAT and environmental fees.
                          """,
                IsActive = true,
                EffectiveFrom = EffectiveFrom,
                CreatedAt = now,
                UpdatedAt = now
            }
        ];
    }

    private readonly record struct TierKey(ServiceType ServiceType, int? RoomCapacity, DateOnly EffectiveFrom, decimal FromUsage);

    private readonly record struct ContractTemplateKey(string Code, int Version);
}
