using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Domain.Repositories;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Repositories;

public class BookingRepository(BookingDbContext dbContext) : IBookingRepository
{
    public void Add(Booking booking)
    {
        dbContext.Bookings.Add(booking);
    }

    public async Task<IReadOnlyList<Booking>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Fees)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Bookings
            .Include(b => b.Fees)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Booking?> GetCurrentRoomBookingByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.UserId == userId &&
                (booking.Status == BookingStatus.Active ||
                 booking.Status == BookingStatus.Confirmed))
            .OrderBy(booking => booking.Status == BookingStatus.Active ? 0 : 1)
            .ThenByDescending(booking => booking.Term.StartDate)
            .ThenByDescending(booking => booking.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetRoomOccupantBookingsAsync(
        Guid roomId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.RoomId == roomId &&
                (booking.Status == BookingStatus.Confirmed ||
                 booking.Status == BookingStatus.Active))
            .OrderBy(booking => booking.Status == BookingStatus.Active ? 0 : 1)
            .ThenBy(booking => booking.Term.StartDate)
            .ThenBy(booking => booking.UserId)
            .ToListAsync(cancellationToken);
    }

    public void Update(Booking booking)
    {
        dbContext.Bookings.Update(booking);
    }
}
