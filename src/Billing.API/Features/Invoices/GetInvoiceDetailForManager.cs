using Billing.API.Infrastructure.Database;
using Billing.API.Infrastructure.Services;
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
                .WithDescription("Required roles: Manager, Admin, or SeniorManager. Gets full invoice detail for review before payment update. Status values are Unpaid, WaitForConfirm, Paid, and Canceled. Response includes invoice type metadata, room location snapshot, current building bank account from RoomService when the room still exists, meter indices, tier snapshots, surcharges, totals, payment metadata, and contract template snapshot id.")
                .RequireAuthorization(policy => policy.RequireRole("Manager", "Admin", "SeniorManager"))
                .Produces<InvoiceDetailResponse>(StatusCodes.Status200OK);
        }
    }

    public sealed class Handler(BillingDbContext dbContext, IRoomBillingClient roomBillingClient)
    {
        public async Task<InvoiceDetailResponse> ExecuteAsync(Guid invoiceId, CancellationToken cancellationToken)
        {
            var invoice = await dbContext.Invoices
                .AsNoTracking()
                .Include(invoice => invoice.Surcharges)
                .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken)
                ?? throw new ApiException("Invoice not found.", StatusCodes.Status404NotFound);

            var room = await roomBillingClient.GetRoomBillingInfoAsync(invoice.RoomId, cancellationToken);
            var buildingBankAccount = room is null
                ? null
                : new BuildingBankAccountResponse(room.BankCode, room.AccountNumber, room.AccountName);

            return InvoiceResponseMapper.ToDetail(invoice, buildingBankAccount);
        }
    }
}
