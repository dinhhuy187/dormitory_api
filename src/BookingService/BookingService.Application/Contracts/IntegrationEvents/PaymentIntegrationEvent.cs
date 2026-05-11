namespace BookingService.Application.Contracts.IntegrationEvents;

public record PaymentSucceededIntegrationEvent(Guid BookingId, DateTime PaidAt);
public record PaymentFailedIntegrationEvent(Guid BookingId, string Reason);