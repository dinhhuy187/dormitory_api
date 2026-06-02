using System.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Entities;
using RoomService.API.Domain.Enum;
using RoomService.API.Infrastructure.Database;
using Shared.Contracts.Booking;

namespace RoomService.API.Features.Rooms;

public sealed class ReserveRoomCapacityCommandConsumer(RoomDbContext dbContext)
    : IConsumer<ReserveRoomCapacityCommand>
{
    public async Task Consume(ConsumeContext<ReserveRoomCapacityCommand> context)
    {
        var command = context.Message;
        var hasActiveTransaction = dbContext.Database.CurrentTransaction is not null;
        await using var transaction = hasActiveTransaction
            ? null
            : await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, context.CancellationToken);

        var existingReservation = await dbContext.RoomReservations
            .AsTracking()
            .FirstOrDefaultAsync(reservation => reservation.BookingId == command.BookingId, context.CancellationToken);

        if (existingReservation is not null)
        {
            await PublishExistingReservationResultAsync(context, existingReservation);
            await dbContext.SaveChangesAsync(context.CancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(context.CancellationToken);
            }
            return;
        }

        var room = await dbContext.Rooms
            .Include(room => room.RoomType)
            .FirstOrDefaultAsync(room => room.Id == command.RoomId, context.CancellationToken);

        if (room is null)
        {
            await PublishFailedAsync(context, "Room does not exist.");
            await dbContext.SaveChangesAsync(context.CancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(context.CancellationToken);
            }
            return;
        }

        if (room.RoomStatus == RoomStatus.MAINTENANCE)
        {
            await PublishFailedAsync(context, "Room is under maintenance.");
            await dbContext.SaveChangesAsync(context.CancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(context.CancellationToken);
            }
            return;
        }

        var capacity = room.RoomType?.Capacity ?? 0;
        if (capacity <= 0 || room.OccupiedCount >= capacity)
        {
            if (room.RoomStatus != RoomStatus.MAINTENANCE)
            {
                room.RoomStatus = RoomStatus.FULL;
            }

            await PublishFailedAsync(context, "Room is full.");
            await dbContext.SaveChangesAsync(context.CancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(context.CancellationToken);
            }
            return;
        }

        var reservation = new RoomReservation
        {
            BookingId = command.BookingId,
            RoomId = command.RoomId,
            StudentId = command.StudentId,
            Status = RoomReservationStatus.Reserved,
            ReservedAt = DateTime.UtcNow
        };

        dbContext.RoomReservations.Add(reservation);
        room.OccupiedCount++;
        room.RoomStatus = room.OccupiedCount >= capacity
            ? RoomStatus.FULL
            : RoomStatus.AVAILABLE;

        await context.Publish(new RoomCapacityReservedEvent(
            command.BookingId,
            command.RoomId,
            command.StudentId,
            room.OccupiedCount,
            capacity,
            reservation.ReservedAt));

        await dbContext.SaveChangesAsync(context.CancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(context.CancellationToken);
        }
    }

    private async Task PublishExistingReservationResultAsync(
        ConsumeContext<ReserveRoomCapacityCommand> context,
        RoomReservation reservation)
    {
        var room = await dbContext.Rooms
            .Include(room => room.RoomType)
            .AsNoTracking()
            .FirstOrDefaultAsync(room => room.Id == reservation.RoomId, context.CancellationToken);

        if (reservation.Status == RoomReservationStatus.Reserved && room is not null)
        {
            await context.Publish(new RoomCapacityReservedEvent(
                context.Message.BookingId,
                reservation.RoomId,
                reservation.StudentId,
                room.OccupiedCount,
                room.RoomType?.Capacity ?? 0,
                reservation.ReservedAt));
            return;
        }

        await PublishFailedAsync(context, "Room reservation already released.");
    }

    private static Task PublishFailedAsync(
        ConsumeContext<ReserveRoomCapacityCommand> context,
        string reason)
    {
        return context.Publish(new RoomCapacityReservationFailedEvent(
            context.Message.BookingId,
            context.Message.RoomId,
            context.Message.StudentId,
            reason,
            DateTime.UtcNow));
    }
}
