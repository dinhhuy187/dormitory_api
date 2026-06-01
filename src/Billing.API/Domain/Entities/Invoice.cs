using Billing.API.Domain.Enums;

namespace Billing.API.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public InvoiceType InvoiceType { get; set; } = InvoiceType.MonthlyUtility;
    public Guid? BookingId { get; set; }
    public Guid RoomId { get; set; }
    public string? BuildingCode { get; set; }
    public int? Floor { get; set; }
    public Guid StudentId { get; set; }
    public string? TermName { get; set; }
    public DateTime? DueAt { get; set; }
    public string? Description { get; set; }
    public short BillingMonth { get; set; }
    public int BillingYear { get; set; }

    public int ElectricityOldIndex { get; set; }
    public int ElectricityNewIndex { get; set; }
    public int ElectricityUsage { get; set; }
    public string ElectricityTierSnapshot { get; set; } = "[]";
    public decimal ElectricityAmount { get; set; }

    public int WaterOldIndex { get; set; }
    public int WaterNewIndex { get; set; }
    public int WaterUsage { get; set; }
    public string WaterTierSnapshot { get; set; } = "[]";
    public decimal WaterAmount { get; set; }

    public decimal SurchargeTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    public DateTime? PaidAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public Guid? ContractTemplateId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ContractTemplate? ContractTemplate { get; set; }
    public virtual ICollection<Surcharge> Surcharges { get; set; } = [];
}
