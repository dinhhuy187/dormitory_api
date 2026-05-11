namespace BookingService.Application.Contracts.IntegrationCommands;

public record CreateInvoiceCommand(Guid BookingId, Guid UserId);