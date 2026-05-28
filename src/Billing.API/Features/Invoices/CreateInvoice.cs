using System.Security.Claims;
using System.Text.Json;
using Billing.API.Domain.Entities;
using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Database;
using Billing.API.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Invoices;

public static class CreateInvoice
{
    public sealed record Command(
        Guid RoomId,
        short Month,
        int Year,
        int ElectricityIndex,
        int WaterIndex,
        IReadOnlyList<SurchargeLine> Surcharges);

    public sealed record SurchargeLine(string Name, decimal Amount);

    public sealed record Response(
        Guid InvoiceId,
        Guid RoomId,
        Guid StudentId,
        short Month,
        int Year,
        int RoomCapacity,
        int ElectricityOldIndex,
        int ElectricityNewIndex,
        decimal ElectricityUsage,
        decimal ElectricitySubtotal,
        decimal ElectricityVatAmount,
        decimal ElectricityAmount,
        int WaterOldIndex,
        int WaterNewIndex,
        decimal WaterUsage,
        decimal WaterAmount,
        decimal SurchargeTotal,
        decimal TotalAmount,
        string Status);

    private sealed record TierSnapshotItem(
        string TierName,
        decimal FromUsage,
        decimal? ToUsage,
        decimal Usage,
        decimal UnitPrice,
        decimal Amount);

    private sealed record CalculationResult(decimal Amount, IReadOnlyList<TierSnapshotItem> Snapshot);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RoomId).NotEmpty();
            RuleFor(x => x.Month).InclusiveBetween((short)1, (short)12);
            RuleFor(x => x.Year).InclusiveBetween(2020, 2100);
            RuleFor(x => x.ElectricityIndex).GreaterThanOrEqualTo(0);
            RuleFor(x => x.WaterIndex).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Surcharges).NotNull();
            RuleForEach(x => x.Surcharges).ChildRules(surcharge =>
            {
                surcharge.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
                surcharge.RuleFor(x => x.Amount).GreaterThan(0);
            });
            RuleFor(x => x.Surcharges)
                .Must(lines => lines is null || lines
                    .GroupBy(line => line.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .All(group => group.Count() == 1))
                .WithMessage("Duplicate surcharge names are not allowed.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/billing/invoices", async (
                    Command command,
                    Handler handler,
                    HttpContext httpContext,
                    CancellationToken ct) =>
                {
                    if (!TryGetCurrentUserId(httpContext.User, out var currentUserId))
                    {
                        return Results.Unauthorized();
                    }

                    var response = await handler.ExecuteAsync(command, currentUserId, ct);
                    return Results.Created($"/api/billing/invoices/{response.InvoiceId}", new ApiResponse<Response>(response));
                })
                .WithTags("Billing - Invoices")
                .WithName("CreateInvoice")
                .RequireAuthorization(policy => policy.RequireRole("Manager", "Admin", "SeniorManager"))
                .AddEndpointFilter<ValidationFilter<Command>>()
                .Produces<ApiResponse<Response>>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized);
        }

        private static bool TryGetCurrentUserId(ClaimsPrincipal user, out Guid userId)
        {
            var rawUserId = user.FindFirstValue("student_id")
                ?? user.FindFirstValue("studentId")
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? user.FindFirstValue("userId");

            return Guid.TryParse(rawUserId, out userId);
        }
    }

    public sealed class Handler(BillingDbContext dbContext, IRoomBillingClient roomBillingClient)
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task<Response> ExecuteAsync(Command command, Guid studentId, CancellationToken cancellationToken)
        {
            var room = await roomBillingClient.GetRoomBillingInfoAsync(command.RoomId, cancellationToken)
                ?? throw new ApiException("Room not found.", StatusCodes.Status404NotFound);

            var invoiceExists = await dbContext.Invoices.AnyAsync(
                invoice => invoice.RoomId == command.RoomId &&
                           invoice.BillingYear == command.Year &&
                           invoice.BillingMonth == command.Month,
                cancellationToken);

            if (invoiceExists)
            {
                throw new ApiException("Invoice already exists for this room and billing period.", StatusCodes.Status409Conflict);
            }

            var previousInvoice = await dbContext.Invoices
                .AsNoTracking()
                .Where(invoice => invoice.RoomId == command.RoomId)
                .OrderByDescending(invoice => invoice.BillingYear)
                .ThenByDescending(invoice => invoice.BillingMonth)
                .ThenByDescending(invoice => invoice.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var electricityOldIndex = previousInvoice?.ElectricityNewIndex ?? 0;
            var waterOldIndex = previousInvoice?.WaterNewIndex ?? 0;

            if (command.ElectricityIndex < electricityOldIndex)
            {
                throw new ApiException("Electricity index must be greater than or equal to the previous index.", StatusCodes.Status400BadRequest);
            }

            if (command.WaterIndex < waterOldIndex)
            {
                throw new ApiException("Water index must be greater than or equal to the previous index.", StatusCodes.Status400BadRequest);
            }

            var electricityUsage = command.ElectricityIndex - electricityOldIndex;
            var waterUsage = command.WaterIndex - waterOldIndex;
            var billingDate = new DateOnly(command.Year, command.Month, 1);

            var electricityTiers = await GetApplicableTiersAsync(ServiceType.Electricity, room.Capacity, billingDate, cancellationToken);
            var waterTiers = await GetApplicableTiersAsync(ServiceType.Water, room.Capacity, billingDate, cancellationToken);

            var electricityCalculation = CalculateTieredAmount(electricityUsage, electricityTiers);
            var electricitySubtotal = electricityCalculation.Amount;
            var electricityVatAmount = Math.Round(electricitySubtotal * 0.08m, 2);
            var electricityAmount = electricitySubtotal + electricityVatAmount;
            var electricitySnapshot = electricityCalculation.Snapshot.ToList();

            if (electricityVatAmount > 0)
            {
                electricitySnapshot.Add(new TierSnapshotItem("VAT 8%", 0, null, 0, 0.08m, electricityVatAmount));
            }

            var waterCalculation = CalculateTieredAmount(waterUsage, waterTiers);
            var waterAmount = waterCalculation.Amount;
            var surchargeTotal = command.Surcharges.Sum(line => line.Amount);
            var totalAmount = electricityAmount + waterAmount + surchargeTotal;
            var now = DateTime.UtcNow;

            var activeContractTemplate = await dbContext.ContractTemplates
                .AsNoTracking()
                .Where(template => template.IsActive &&
                                   template.EffectiveFrom <= billingDate &&
                                   (template.EffectiveTo == null || template.EffectiveTo >= billingDate))
                .OrderByDescending(template => template.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);

            var invoice = new Invoice
            {
                RoomId = command.RoomId,
                StudentId = studentId,
                BillingMonth = command.Month,
                BillingYear = command.Year,
                ElectricityOldIndex = electricityOldIndex,
                ElectricityNewIndex = command.ElectricityIndex,
                ElectricityUsage = electricityUsage,
                ElectricityTierSnapshot = JsonSerializer.Serialize(electricitySnapshot, JsonOptions),
                ElectricityAmount = electricityAmount,
                WaterOldIndex = waterOldIndex,
                WaterNewIndex = command.WaterIndex,
                WaterUsage = waterUsage,
                WaterTierSnapshot = JsonSerializer.Serialize(waterCalculation.Snapshot, JsonOptions),
                WaterAmount = waterAmount,
                SurchargeTotal = surchargeTotal,
                TotalAmount = totalAmount,
                Status = InvoiceStatus.Unpaid,
                ContractTemplateId = activeContractTemplate?.Id,
                CreatedAt = now,
                UpdatedAt = now
            };

            foreach (var surcharge in command.Surcharges)
            {
                invoice.Surcharges.Add(new Surcharge
                {
                    Name = surcharge.Name.Trim(),
                    Amount = surcharge.Amount,
                    CreatedAt = now
                });
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            dbContext.Invoices.Add(invoice);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new Response(
                invoice.Id,
                invoice.RoomId,
                invoice.StudentId,
                invoice.BillingMonth,
                invoice.BillingYear,
                room.Capacity,
                invoice.ElectricityOldIndex,
                invoice.ElectricityNewIndex,
                invoice.ElectricityUsage,
                electricitySubtotal,
                electricityVatAmount,
                invoice.ElectricityAmount,
                invoice.WaterOldIndex,
                invoice.WaterNewIndex,
                invoice.WaterUsage,
                invoice.WaterAmount,
                invoice.SurchargeTotal,
                invoice.TotalAmount,
                invoice.Status.ToString());
        }

        private async Task<IReadOnlyList<ServicePriceTier>> GetApplicableTiersAsync(
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

        private static CalculationResult CalculateTieredAmount(decimal usage, IReadOnlyList<ServicePriceTier> tiers)
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
}
