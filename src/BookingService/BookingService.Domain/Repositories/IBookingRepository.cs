using BookingService.Domain.Entities;

namespace BookingService.Domain.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken); 
    Task<IReadOnlyList<Booking>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    void Add(Booking booking);
    void Update(Booking booking);
}