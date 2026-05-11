using BookingService.Application.Abtractions.Services;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Services;

public class RoomPricingService(BookingDbContext dbContext) : IRoomPricingService
{
    public async Task<decimal> GetMonthlyPriceAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await dbContext.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);

        return room?.MonthlyPrice ?? 0;
    }
}