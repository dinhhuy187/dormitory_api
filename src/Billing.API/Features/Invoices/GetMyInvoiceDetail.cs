using Billing.API.Infrastructure.Auth;
using Billing.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Invoices;

public static class GetMyInvoiceDetail
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/billing/invoices/me/{invoiceId:guid}", async (
                    Guid invoiceId,
                    Handler handler,
                    HttpContext httpContext,
                    CancellationToken ct) =>
                {
                    if (!CurrentUser.TryGetUserId(httpContext.User, out var studentId))
                    {
                        return Results.Unauthorized();
                    }

                    var response = await handler.ExecuteAsync(invoiceId, studentId, ct);
                    return Results.Ok(new ApiResponse<InvoiceDetailResponse>(response));
                })
                .WithTags("Billing - Invoices")
                .WithName("GetMyInvoiceDetail")
                .WithDescription("Required role: Student. Gets full invoice detail for the authenticated student only. Status values are Unpaid, Paid, and Canceled. Response includes room location snapshot, old/new meter indices, tier snapshots, surcharges, totals, payment status, and contract template snapshot id.")
                .RequireAuthorization(policy => policy.RequireRole("Student"))
                .Produces<InvoiceDetailResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized);
        }
    }

    public sealed class Handler(BillingDbContext dbContext)
    {
        public async Task<InvoiceDetailResponse> ExecuteAsync(Guid invoiceId, Guid studentId, CancellationToken cancellationToken)
        {
            var invoice = await dbContext.Invoices
                .AsNoTracking()
                .Include(invoice => invoice.Surcharges)
                .FirstOrDefaultAsync(
                    invoice => invoice.Id == invoiceId && invoice.StudentId == studentId,
                    cancellationToken)
                ?? throw new ApiException("Invoice not found.", StatusCodes.Status404NotFound);

            return InvoiceResponseMapper.ToDetail(invoice);
        }
    }
}
