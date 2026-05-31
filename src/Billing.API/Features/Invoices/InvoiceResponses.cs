using System.Text.Json;
using Billing.API.Domain.Entities;

namespace Billing.API.Features.Invoices;

public sealed record InvoiceListItemResponse(
    Guid InvoiceId,
    Guid RoomId,
    short Month,
    int Year,
    decimal TotalAmount,
    string Status,
    DateTime? PaidAt,
    DateTime CreatedAt);

public sealed record InvoiceDetailResponse(
    Guid InvoiceId,
    Guid RoomId,
    string? BuildingCode,
    int? Floor,
    Guid StudentId,
    short Month,
    int Year,
    int ElectricityOldIndex,
    int ElectricityNewIndex,
    int ElectricityUsage,
    IReadOnlyList<TierSnapshotResponse> ElectricityTierSnapshot,
    decimal ElectricityAmount,
    int WaterOldIndex,
    int WaterNewIndex,
    int WaterUsage,
    IReadOnlyList<TierSnapshotResponse> WaterTierSnapshot,
    decimal WaterAmount,
    IReadOnlyList<SurchargeResponse> Surcharges,
    decimal SurchargeTotal,
    decimal TotalAmount,
    string Status,
    DateTime? PaidAt,
    Guid? UpdatedByUserId,
    Guid? ContractTemplateId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record TierSnapshotResponse(
    string TierName,
    decimal FromUsage,
    decimal? ToUsage,
    decimal Usage,
    decimal UnitPrice,
    decimal Amount);

public sealed record SurchargeResponse(Guid Id, string Name, decimal Amount, DateTime CreatedAt);

public static class InvoiceResponseMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static InvoiceListItemResponse ToListItem(Invoice invoice)
    {
        return new InvoiceListItemResponse(
            invoice.Id,
            invoice.RoomId,
            invoice.BillingMonth,
            invoice.BillingYear,
            invoice.TotalAmount,
            invoice.Status.ToString(),
            invoice.PaidAt,
            invoice.CreatedAt);
    }

    public static InvoiceDetailResponse ToDetail(Invoice invoice)
    {
        return new InvoiceDetailResponse(
            invoice.Id,
            invoice.RoomId,
            invoice.BuildingCode,
            invoice.Floor,
            invoice.StudentId,
            invoice.BillingMonth,
            invoice.BillingYear,
            invoice.ElectricityOldIndex,
            invoice.ElectricityNewIndex,
            invoice.ElectricityUsage,
            DeserializeSnapshot(invoice.ElectricityTierSnapshot),
            invoice.ElectricityAmount,
            invoice.WaterOldIndex,
            invoice.WaterNewIndex,
            invoice.WaterUsage,
            DeserializeSnapshot(invoice.WaterTierSnapshot),
            invoice.WaterAmount,
            invoice.Surcharges
                .OrderBy(surcharge => surcharge.CreatedAt)
                .Select(surcharge => new SurchargeResponse(
                    surcharge.Id,
                    surcharge.Name,
                    surcharge.Amount,
                    surcharge.CreatedAt))
                .ToList(),
            invoice.SurchargeTotal,
            invoice.TotalAmount,
            invoice.Status.ToString(),
            invoice.PaidAt,
            invoice.UpdatedByUserId,
            invoice.ContractTemplateId,
            invoice.CreatedAt,
            invoice.UpdatedAt);
    }

    private static IReadOnlyList<TierSnapshotResponse> DeserializeSnapshot(string snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<TierSnapshotResponse>>(snapshot, JsonOptions) ?? [];
    }
}
