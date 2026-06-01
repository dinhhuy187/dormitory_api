using System.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Enum;
using RoomService.API.Infrastructure.Database;
using Shared.Contracts.Booking;

namespace RoomService.API.Features.Rooms;

public sealed class ReleaseRoomCapacityCommandConsumer(RoomDbContext dbContext)
    : IConsumer<ReleaseRoomCapacityCommand>
{
    public async Task Consume(ConsumeContext<ReleaseRoomCapacityCommand> context)
    {
        var command = context.Message;
        var hasActiveTransaction = dbContext.Database.CurrentTransaction is not null;
        await using var transaction = hasActiveTransaction
            ? null
            : await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, context.CancellationToken);

        var reservation = await dbContext.RoomReservations
            .FirstOrDefaultAsync(reservation => reservation.BookingId == command.BookingId, context.CancellationToken);

        if (reservation is null)
        {
            await PublishCurrentRoomStateIfAvailableAsync(context);
            await dbContext.SaveChangesAsync(context.CancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(context.CancellationToken);
            }
            return;
        }

        var room = await dbContext.Rooms
            .Include(room => room.RoomType)
            .FirstOrDefaultAsync(room => room.Id == reservation.RoomId, context.CancellationToken);

        if (room is null)
        {
            if (transaction is not null)
            {
                await transaction.CommitAsync(context.CancellationToken);
            }
            return;
        }

        if (reservation.Status == RoomReservationStatus.Released)
        {
            await PublishReleasedAsync(context, room, reservation.ReleasedAt ?? DateTime.UtcNow);
            await dbContext.SaveChangesAsync(context.CancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(context.CancellationToken);
            }
            return;
        }

        room.OccupiedCount = Math.Max(0, room.OccupiedCount - 1);
        if (room.RoomStatus != RoomStatus.MAINTENANCE &&
            room.OccupiedCount < (room.RoomType?.Capacity ?? 0))
        {
            room.RoomStatus = RoomStatus.AVAILABLE;
        }

        reservation.Status = RoomReservationStatus.Released;
        reservation.ReleasedAt = DateTime.UtcNow;
        reservation.ReleaseReason = command.Reason;

        await PublishReleasedAsync(context, room, reservation.ReleasedAt.Value);
        await dbContext.SaveChangesAsync(context.CancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(context.CancellationToken);
        }
    }

    private async Task PublishCurrentRoomStateIfAvailableAsync(ConsumeContext<ReleaseRoomCapacityCommand> context)
    {
        var room = await dbContext.Rooms
            .Include(room => room.RoomType)
            .AsNoTracking()
            .FirstOrDefaultAsync(room => room.Id == context.Message.RoomId, context.CancellationToken);

        if (room is not null)
        {
            await PublishReleasedAsync(context, room, DateTime.UtcNow);
        }
    }

    private static Task PublishReleasedAsync(
        ConsumeContext<ReleaseRoomCapacityCommand> context,
        Domain.Entities.Room room,
        DateTime releasedAt)
    {
        return context.Publish(new RoomCapacityReleasedEvent(
            context.Message.BookingId,
            room.Id,
            context.Message.StudentId,
            room.OccupiedCount,
            room.RoomType?.Capacity ?? 0,
            releasedAt));
    }
}
