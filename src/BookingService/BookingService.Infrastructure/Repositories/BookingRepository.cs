using BookingService.Domain.Entities;
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

    public void Update(Booking booking)
    {
        dbContext.Bookings.Update(booking);
    }
}