using BookingService.Domain.Enums;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingService.API.Endpoints;

public static class BookingSyncEndpoints
{
    public static void MapBookingSyncEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bookings/sync", async (
                BookingDbContext dbContext,
                CancellationToken ct) =>
            {
                var bookings = await dbContext.Bookings
                    .AsNoTracking()
                    .Where(booking => booking.Status == BookingStatus.Confirmed ||
                                      booking.Status == BookingStatus.Active)
                    .OrderBy(booking => booking.Term.StartDate)
                    .ThenBy(booking => booking.RoomId)
                    .ThenBy(booking => booking.UserId)
                    .Select(booking => new BookingBillingSyncDto(
                        booking.Id,
                        booking.RoomId,
                        booking.UserId,
                        booking.Term.TermName,
                        booking.Term.StartDate,
                        booking.Term.EndDate,
                        booking.Term.NumberOfMonths,
                        booking.PricePerMonth,
                        booking.Status.ToString(),
                        booking.CreatedAt))
                    .ToListAsync(ct);

                return Results.Ok(bookings);
            })
            .WithTags("Bookings - Sync")
            .WithName("SyncBookingsForBilling")
            .WithDescription("Internal service-to-service sync endpoint for Billing seed data. Returns confirmed and active bookings as a raw JSON list and does not require a user JWT.")
            .Produces<IReadOnlyList<BookingBillingSyncDto>>(StatusCodes.Status200OK);
    }
}

public sealed record BookingBillingSyncDto(
    Guid BookingId,
    Guid RoomId,
    Guid StudentId,
    string TermName,
    DateTime StartDate,
    DateTime EndDate,
    int NumberOfMonths,
    decimal PricePerMonth,
    string Status,
    DateTime CreatedAt);
