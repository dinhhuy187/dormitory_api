using BookingService.Domain.Events;
using MassTransit;
using Shared.Contracts.Booking;

namespace BookingService.Infrastructure.EventHandlers.Internal;

public sealed class StudentCheckedOutDomainEventConsumer : IConsumer<StudentCheckedOutDomainEvent>
{
    public async Task Consume(ConsumeContext<StudentCheckedOutDomainEvent> context)
    {
        var message = context.Message;

        await context.Publish(new ReleaseRoomCapacityCommand(
            message.BookingId,
            message.RoomId,
            message.UserId,
            "Student checked out.",
            DateTime.UtcNow));
    }
}
