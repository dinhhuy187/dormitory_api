using Billing.API.Domain.Enums;

namespace Billing.API.Domain.Entities;

public class ServicePriceTier
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ServiceType ServiceType { get; set; }
    public string TierName { get; set; } = string.Empty;
    public int? RoomCapacity { get; set; }
    public decimal FromUsage { get; set; }
    public decimal? ToUsage { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
