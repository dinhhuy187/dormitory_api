using Billing.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Invoices;

public static class GetInvoiceDetailForManager
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/billing/invoices/{invoiceId:guid}", async (
                    Guid invoiceId,
                    Handler handler,
                    CancellationToken ct) =>
                {
                    var response = await handler.ExecuteAsync(invoiceId, ct);
                    return Results.Ok(new ApiResponse<InvoiceDetailResponse>(response));
                })
                .WithTags("Billing - Invoices")
                .WithName("GetInvoiceDetailForManager")
                .WithDescription("Required roles: Manager, Admin, or SeniorManager. Gets full invoice detail for review before payment update. Status values are Unpaid and Paid. Response includes meter indices, tier snapshots, surcharges, totals, payment metadata, and contract template snapshot id.")
                .RequireAuthorization(policy => policy.RequireRole("Manager", "Admin", "SeniorManager"))
                .Produces<InvoiceDetailResponse>(StatusCodes.Status200OK);
        }
    }

    public sealed class Handler(BillingDbContext dbContext)
    {
        public async Task<InvoiceDetailResponse> ExecuteAsync(Guid invoiceId, CancellationToken cancellationToken)
        {
            var invoice = await dbContext.Invoices
                .AsNoTracking()
                .Include(invoice => invoice.Surcharges)
                .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken)
                ?? throw new ApiException("Invoice not found.", StatusCodes.Status404NotFound);

            return InvoiceResponseMapper.ToDetail(invoice);
        }
    }
}
