namespace BookingService.Application.Common.Models;

public interface IConfirmBookingUseCase
{
    Task<Result<bool>> ExecuteAsync(Guid bookingId, CancellationToken cancellationToken);
}