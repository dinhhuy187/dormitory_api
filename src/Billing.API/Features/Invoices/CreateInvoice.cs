using System.Text.Json;
using Billing.API.Domain.Entities;
using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Auth;
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
        Guid? StudentId,
        short Month,
        int Year,
        int RoomCapacity,
        string BuildingCode,
        int Floor,
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
                    if (!CurrentUser.TryGetUserId(httpContext.User, out var currentUserId))
                    {
                        return Results.Unauthorized();
                    }

                    var response = await handler.ExecuteAsync(command, currentUserId, ct);
                    return Results.Created($"/api/billing/invoices/{response.InvoiceId}", new ApiResponse<Response>(response));
                })
                .WithTags("Billing - Invoices")
                .WithName("CreateInvoice")
                .WithDescription("Required roles: Manager, Admin, or SeniorManager. Creates a monthly invoice for a room. Room existence, room type, capacity, building code, and floor are resolved through RoomService gRPC; building code and floor are stored as invoice snapshots. Old meter indices are derived from the latest non-canceled room invoice. A room-type-specific contract template snapshot is linked when available, with generic fallback. New invoice status is Unpaid. Status values are Unpaid, Paid, and Canceled. Electricity receives 8% VAT; water prices already include fees and tax.")
                .RequireAuthorization(policy => policy.RequireRole("Manager", "Admin", "SeniorManager"))
                .AddEndpointFilter<ValidationFilter<Command>>()
                .Produces<Response>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized);
        }
    }

    public sealed class Handler(BillingDbContext dbContext, IRoomBillingClient roomBillingClient)
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task<Response> ExecuteAsync(Command command, Guid currentUserId, CancellationToken cancellationToken)
        {
            var room = await roomBillingClient.GetRoomBillingInfoAsync(command.RoomId, cancellationToken)
                ?? throw new ApiException("Room not found.", StatusCodes.Status404NotFound);

            var invoiceExists = await dbContext.Invoices.AnyAsync(
                invoice => invoice.RoomId == command.RoomId &&
                           invoice.BillingYear == command.Year &&
                           invoice.BillingMonth == command.Month &&
                           invoice.InvoiceType == InvoiceType.MonthlyUtility,
                cancellationToken);

            if (invoiceExists)
            {
                throw new ApiException("Invoice already exists for this room and billing period.", StatusCodes.Status409Conflict);
            }

            var previousInvoice = await dbContext.Invoices
                .AsNoTracking()
                .Where(invoice => invoice.RoomId == command.RoomId &&
                                  invoice.InvoiceType == InvoiceType.MonthlyUtility &&
                                  invoice.Status != InvoiceStatus.Canceled)
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

            var electricityTiers = await InvoiceCalculationHelper.GetApplicableTiersAsync(dbContext, ServiceType.Electricity, room.Capacity, billingDate, cancellationToken);
            var waterTiers = await InvoiceCalculationHelper.GetApplicableTiersAsync(dbContext, ServiceType.Water, room.Capacity, billingDate, cancellationToken);

            var electricityCalculation = InvoiceCalculationHelper.CalculateTieredAmount(electricityUsage, electricityTiers);
            var electricitySubtotal = electricityCalculation.Amount;
            var electricityVatAmount = Math.Round(electricitySubtotal * 0.08m, 2);
            var electricityAmount = electricitySubtotal + electricityVatAmount;
            var electricitySnapshot = electricityCalculation.Snapshot.ToList();

            if (electricityVatAmount > 0)
            {
                electricitySnapshot.Add(new InvoiceCalculationHelper.TierSnapshotItem("VAT 8%", 0, null, 0, 0.08m, electricityVatAmount));
            }

            var waterCalculation = InvoiceCalculationHelper.CalculateTieredAmount(waterUsage, waterTiers);
            var waterAmount = waterCalculation.Amount;
            var surchargeTotal = command.Surcharges.Sum(line => line.Amount);
            var totalAmount = electricityAmount + waterAmount + surchargeTotal;
            var now = DateTime.UtcNow;

            var activeContractTemplate = await dbContext.ContractTemplates
                .AsNoTracking()
                .Where(template => template.IsActive &&
                                   template.RoomTypeId == room.RoomTypeId &&
                                   template.EffectiveFrom <= billingDate &&
                                   (template.EffectiveTo == null || template.EffectiveTo >= billingDate))
                .OrderByDescending(template => template.EffectiveFrom)
                .ThenByDescending(template => template.Version)
                .FirstOrDefaultAsync(cancellationToken)
                ?? await dbContext.ContractTemplates
                    .AsNoTracking()
                    .Where(template => template.IsActive &&
                                       template.RoomTypeId == null &&
                                       template.EffectiveFrom <= billingDate &&
                                       (template.EffectiveTo == null || template.EffectiveTo >= billingDate))
                    .OrderByDescending(template => template.EffectiveFrom)
                    .ThenByDescending(template => template.Version)
                    .FirstOrDefaultAsync(cancellationToken);

            var invoice = new Invoice
            {
                RoomId = command.RoomId,
                BuildingCode = room.BuildingCode.Trim(),
                Floor = room.Floor,
                StudentId = null,
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
                room.BuildingCode,
                room.Floor,
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

    }
}
