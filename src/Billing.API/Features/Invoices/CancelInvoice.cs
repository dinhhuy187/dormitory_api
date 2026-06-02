using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Auth;
using Billing.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Invoices;

public static class CancelInvoice
{
    public sealed record Response(
        Guid InvoiceId,
        string Status,
        DateTime CanceledAt,
        Guid UpdatedByUserId);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/api/billing/invoices/{invoiceId:guid}", async (
                    Guid invoiceId,
                    Handler handler,
                    HttpContext httpContext,
                    CancellationToken ct) =>
                {
                    if (!CurrentUser.TryGetUserId(httpContext.User, out var updatedByUserId))
                    {
                        return Results.Unauthorized();
                    }

                    var response = await handler.ExecuteAsync(invoiceId, updatedByUserId, ct);
                    return Results.Ok(new ApiResponse<Response>(response));
                })
                .WithTags("Billing - Invoices")
                .WithName("CancelInvoice")
                .WithDescription("Required roles: Manager, Admin, or SeniorManager. Soft-cancels an invoice; DELETE does not hard delete the database row, surcharge lines, or tier snapshots. Only Unpaid invoices can be canceled. Status values are Unpaid, WaitForConfirm, Paid, and Canceled.")
                .RequireAuthorization(policy => policy.RequireRole("Manager", "Admin", "SeniorManager"))
                .Produces<Response>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized);
        }
    }

    public sealed class Handler(BillingDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(Guid invoiceId, Guid updatedByUserId, CancellationToken cancellationToken)
        {
            var invoice = await dbContext.Invoices
                .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken)
                ?? throw new ApiException("Invoice not found.", StatusCodes.Status404NotFound);

            if (invoice.Status == InvoiceStatus.Paid)
            {
                throw new ApiException("Paid invoices cannot be canceled.", StatusCodes.Status409Conflict);
            }

            if (invoice.Status == InvoiceStatus.Canceled)
            {
                throw new ApiException("Invoice has already been canceled.", StatusCodes.Status409Conflict);
            }

            if (invoice.Status == InvoiceStatus.WaitForConfirm)
            {
                throw new ApiException("Invoices waiting for payment confirmation cannot be canceled.", StatusCodes.Status409Conflict);
            }

            if (invoice.Status != InvoiceStatus.Unpaid)
            {
                throw new ApiException("Only unpaid invoices can be canceled.", StatusCodes.Status409Conflict);
            }

            var now = DateTime.UtcNow;
            invoice.Status = InvoiceStatus.Canceled;
            invoice.UpdatedByUserId = updatedByUserId;
            invoice.UpdatedAt = now;

            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response(invoice.Id, invoice.Status.ToString(), now, updatedByUserId);
        }
    }
}
