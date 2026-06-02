using BookingService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Booking;

namespace BookingService.Infrastructure.EventHandlers.Integration;

public sealed class RoomCapacityReleasedProjectionConsumer(BookingDbContext dbContext)
    : IConsumer<RoomCapacityReleasedEvent>
{
    public async Task Consume(ConsumeContext<RoomCapacityReleasedEvent> context)
    {
        var message = context.Message;
        var room = await dbContext.Rooms
            .FirstOrDefaultAsync(room => room.Id == message.RoomId, context.CancellationToken);

        if (room is null)
        {
            return;
        }

        room.OccupiedCount = message.OccupiedCount;
        room.Capacity = message.Capacity;
        room.Status = message.OccupiedCount >= message.Capacity ? "FULL" : "AVAILABLE";

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
