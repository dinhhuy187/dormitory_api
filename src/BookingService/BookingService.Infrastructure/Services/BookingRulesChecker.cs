using BookingService.Domain.Enums;
using BookingService.Domain.Services;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Services;

public class BookingRulesChecker(BookingDbContext dbContext) : IBookingRulesChecker
{
    public async Task<bool> HasExceededActiveBookingLimitAsync(Guid userId, string termName, CancellationToken cancellationToken)
    {
        var hasActiveBooking = await dbContext.Bookings
            .AsNoTracking()
            .AnyAsync(b => 
                b.UserId == userId && 
                b.Term.TermName == termName && 
                b.Status != BookingStatus.Canceled, 
                cancellationToken);

        return hasActiveBooking;
    }

    public async Task<bool> IsRoomAvailableForBookingAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await dbContext.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);

        // 1. Nếu không tìm thấy phòng
        if (room == null) return false;

        // 2. Nếu phòng đang bảo trì hoặc đã được đánh dấu là Full
        if (room.Status == "MAINTENANCE" || room.Status == "FULL") return false;

        // 3. Kiểm tra logic sức chứa an toàn (Guard check thêm)
        if (room.OccupiedCount >= room.Capacity) return false;

        // Hoàn toàn hợp lệ để đặt
        return true;
    }
}