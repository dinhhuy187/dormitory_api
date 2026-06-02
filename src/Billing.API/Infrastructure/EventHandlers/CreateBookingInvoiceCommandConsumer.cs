using Billing.API.Domain.Entities;
using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Database;
using Billing.API.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Booking;

namespace Billing.API.Infrastructure.EventHandlers;

public sealed class CreateBookingInvoiceCommandConsumer(
    BillingDbContext dbContext,
    IRoomBillingClient roomBillingClient,
    ILogger<CreateBookingInvoiceCommandConsumer> logger) : IConsumer<CreateInvoiceCommand>
{
    public async Task Consume(ConsumeContext<CreateInvoiceCommand> context)
    {
        var command = context.Message;
        var existingInvoice = await dbContext.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(invoice =>
                invoice.InvoiceType == InvoiceType.BookingRegistration &&
                invoice.BookingId == command.BookingId,
                context.CancellationToken);

        if (existingInvoice is not null)
        {
            await PublishInvoiceCreatedAsync(context, existingInvoice);
            return;
        }

        var room = await roomBillingClient.GetRoomBillingInfoAsync(command.RoomId, context.CancellationToken)
            ?? throw new InvalidOperationException($"Room {command.RoomId} was not found.");

        var expectedTotal = command.BasePrice + command.Fees.Sum(fee => fee.Amount);
        if (expectedTotal != command.TotalPrice)
        {
            logger.LogWarning(
                "Booking invoice total mismatch for booking {BookingId}. Expected {ExpectedTotal}, snapshot total {SnapshotTotal}.",
                command.BookingId,
                expectedTotal,
                command.TotalPrice);
        }

        var now = DateTime.UtcNow;
        var invoice = new Invoice
        {
            InvoiceType = InvoiceType.BookingRegistration,
            BookingId = command.BookingId,
            RoomId = command.RoomId,
            BuildingCode = room.BuildingCode,
            Floor = room.Floor,
            StudentId = command.StudentId,
            TermName = command.TermName,
            DueAt = command.PaymentDueAt,
            Description = $"Booking registration invoice for {command.TermName}",
            BillingMonth = (short)command.BookingCreatedAt.Month,
            BillingYear = command.BookingCreatedAt.Year,
            ElectricityOldIndex = 0,
            ElectricityNewIndex = 0,
            ElectricityUsage = 0,
            ElectricityAmount = 0m,
            WaterOldIndex = 0,
            WaterNewIndex = 0,
            WaterUsage = 0,
            WaterAmount = 0m,
            SurchargeTotal = command.TotalPrice,
            TotalAmount = command.TotalPrice,
            Status = InvoiceStatus.Unpaid,
            CreatedAt = now,
            UpdatedAt = now
        };

        invoice.Surcharges.Add(new Surcharge
        {
            Name = $"Room fee {command.TermName}".Trim(),
            Amount = command.BasePrice,
            CreatedAt = now
        });

        foreach (var fee in command.Fees.Where(fee => fee.Amount > 0))
        {
            invoice.Surcharges.Add(new Surcharge
            {
                Name = fee.FeeName.Trim(),
                Amount = fee.Amount,
                CreatedAt = now
            });
        }

        dbContext.Invoices.Add(invoice);
        await PublishInvoiceCreatedAsync(context, invoice);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }

    private static Task PublishInvoiceCreatedAsync(
        ConsumeContext<CreateInvoiceCommand> context,
        Invoice invoice)
    {
        return context.Publish(new BookingInvoiceCreatedEvent(
            invoice.BookingId!.Value,
            invoice.Id,
            invoice.StudentId!.Value,
            invoice.RoomId,
            invoice.TotalAmount,
            invoice.DueAt ?? context.Message.PaymentDueAt,
            invoice.CreatedAt));
    }
}
