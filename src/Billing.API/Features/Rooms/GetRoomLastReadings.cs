using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Database;
using Billing.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Rooms;

public static class GetRoomLastReadings
{
    public sealed record Response(
        Guid RoomId,
        string RoomNumber,
        string BuildingCode,
        int Floor,
        int ElectricityOldIndex,
        int WaterOldIndex,
        bool HasPreviousInvoice,
        Guid? SourceInvoiceId,
        short? SourceMonth,
        int? SourceYear,
        DateTime? SourceCreatedAt);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/billing/rooms/{roomId:guid}/last-readings", async (
                    Guid roomId,
                    Handler handler,
                    CancellationToken ct) =>
                {
                    var response = await handler.ExecuteAsync(roomId, ct);
                    return Results.Ok(new ApiResponse<Response>(response));
                })
                .WithTags("Billing - Rooms")
                .WithName("GetRoomLastReadings")
                .WithDescription("Required roles: Manager, Admin, or SeniorManager. Returns the old electricity and water indices to use when creating the next invoice for a room. Room existence and location are resolved through RoomService gRPC. Readings come from the latest non-canceled invoice; if none exists, both readings are returned as 0.")
                .RequireAuthorization(policy => policy.RequireRole("Manager", "Admin", "SeniorManager"))
                .Produces<Response>(StatusCodes.Status200OK);
        }
    }

    public sealed class Handler(BillingDbContext dbContext, IRoomBillingClient roomBillingClient)
    {
        public async Task<Response> ExecuteAsync(Guid roomId, CancellationToken cancellationToken)
        {
            var room = await roomBillingClient.GetRoomBillingInfoAsync(roomId, cancellationToken)
                ?? throw new ApiException("Room not found.", StatusCodes.Status404NotFound);

            var latestInvoice = await dbContext.Invoices
                .AsNoTracking()
                .Where(invoice => invoice.RoomId == roomId &&
                                  invoice.InvoiceType == InvoiceType.MonthlyUtility &&
                                  invoice.Status != InvoiceStatus.Canceled)
                .OrderByDescending(invoice => invoice.BillingYear)
                .ThenByDescending(invoice => invoice.BillingMonth)
                .ThenByDescending(invoice => invoice.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            return new Response(
                room.RoomId,
                room.RoomNumber,
                room.BuildingCode,
                room.Floor,
                latestInvoice?.ElectricityNewIndex ?? 0,
                latestInvoice?.WaterNewIndex ?? 0,
                latestInvoice is not null,
                latestInvoice?.Id,
                latestInvoice?.BillingMonth,
                latestInvoice?.BillingYear,
                latestInvoice?.CreatedAt);
        }
    }
}
