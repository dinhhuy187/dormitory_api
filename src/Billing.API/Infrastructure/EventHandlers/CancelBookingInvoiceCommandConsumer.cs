using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Booking;

namespace Billing.API.Infrastructure.EventHandlers;

public sealed class CancelBookingInvoiceCommandConsumer(
    BillingDbContext dbContext,
    ILogger<CancelBookingInvoiceCommandConsumer> logger) : IConsumer<CancelBookingInvoiceCommand>
{
    public async Task Consume(ConsumeContext<CancelBookingInvoiceCommand> context)
    {
        var command = context.Message;
        var invoice = await dbContext.Invoices
            .FirstOrDefaultAsync(invoice =>
                invoice.InvoiceType == InvoiceType.BookingRegistration &&
                invoice.BookingId == command.BookingId,
                context.CancellationToken);

        if (invoice is null)
        {
            return;
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            logger.LogWarning(
                "Booking invoice {InvoiceId} for booking {BookingId} is already paid and cannot be canceled.",
                invoice.Id,
                command.BookingId);
            return;
        }

        if (invoice.Status != InvoiceStatus.Canceled)
        {
            invoice.Status = InvoiceStatus.Canceled;
            invoice.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }

        await context.Publish(new BookingInvoiceCanceledEvent(
            command.BookingId,
            invoice.Id,
            command.Reason,
            invoice.UpdatedAt));
    }
}
