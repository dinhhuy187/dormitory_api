using MassTransit;

namespace BookingService.Infrastructure.Sagas;

public class BookingSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; } 
    public string CurrentState { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid RoomId { get; set; }
    public DateTime CreatedAt { get; set; }
}