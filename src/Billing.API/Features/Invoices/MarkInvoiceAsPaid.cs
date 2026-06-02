using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Auth;
using Billing.API.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using Shared.Contracts.Booking;

namespace Billing.API.Features.Invoices;

public static class MarkInvoiceAsPaid
{
    public sealed record Response(
        Guid InvoiceId,
        string Status,
        DateTime PaidAt,
        Guid UpdatedByUserId,
        decimal TotalAmount);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/api/billing/invoices/{invoiceId:guid}/payment", async (
                    Guid invoiceId,
                    Handler handler,
                    HttpContext httpContext,
                    CancellationToken ct) =>
                {
                    if (!CurrentUser.TryGetUserId(httpContext.User, out var managerId))
                    {
                        return Results.Unauthorized();
                    }

                    var response = await handler.ExecuteAsync(invoiceId, managerId, ct);
                    return Results.Ok(new ApiResponse<Response>(response));
                })
                .WithTags("Billing - Invoices")
                .WithName("MarkInvoiceAsPaid")
                .WithDescription("Required roles: Manager, Admin, or SeniorManager. Marks an invoice as paid. Allowed state transitions are Unpaid to Paid and WaitForConfirm to Paid; Canceled invoices cannot be paid. Status values are Unpaid, WaitForConfirm, Paid, and Canceled. PaidAt is set to current UTC time and UpdatedByUserId is taken from the authenticated JWT user id.")
                .RequireAuthorization(policy => policy.RequireRole("Manager", "Admin", "SeniorManager"))
                .Produces<Response>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized);
        }
    }

    public sealed class Handler(BillingDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        public async Task<Response> ExecuteAsync(Guid invoiceId, Guid updatedByUserId, CancellationToken cancellationToken)
        {
            var invoice = await dbContext.Invoices
                .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken)
                ?? throw new ApiException("Invoice not found.", StatusCodes.Status404NotFound);

            if (invoice.Status == InvoiceStatus.Paid)
            {
                throw new ApiException("Invoice has already been paid.", StatusCodes.Status409Conflict);
            }

            if (invoice.Status == InvoiceStatus.Canceled)
            {
                throw new ApiException("Canceled invoices cannot be marked as paid.", StatusCodes.Status409Conflict);
            }

            if (invoice.Status != InvoiceStatus.Unpaid && invoice.Status != InvoiceStatus.WaitForConfirm)
            {
                throw new ApiException("Only unpaid invoices or invoices waiting for confirmation can be marked as paid.", StatusCodes.Status409Conflict);
            }

            var now = DateTime.UtcNow;
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = now;
            invoice.UpdatedByUserId = updatedByUserId;
            invoice.UpdatedAt = now;


            if (invoice.InvoiceType == InvoiceType.BookingRegistration && invoice.BookingId is not null)
            {
                await publishEndpoint.Publish(new PaymentSucceededIntegrationEvent(
                    invoice.BookingId.Value,
                    invoice.Id,
                    now), cancellationToken);
            }
            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response(
                invoice.Id,
                invoice.Status.ToString(),
                now,
                updatedByUserId,
                invoice.TotalAmount);
        }
    }
}
