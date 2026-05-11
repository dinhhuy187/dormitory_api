namespace BookingService.Application.Contracts.IntegrationCommands;

public record ReleaseRoomCapacityCommand(Guid RoomId);