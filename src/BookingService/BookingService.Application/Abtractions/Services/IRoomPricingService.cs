namespace BookingService.Application.Abtractions.Services;

public interface IRoomPricingService
{
    Task<decimal> GetMonthlyPriceAsync(Guid roomId, CancellationToken cancellationToken);    
}