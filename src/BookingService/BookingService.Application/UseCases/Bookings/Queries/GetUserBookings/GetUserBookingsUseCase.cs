using BookingService.Application.Common;
using BookingService.Application.Common.Models;
using BookingService.Domain.Repositories;

namespace BookingService.Application.UseCases.Bookings.Queries.GetUserBookings;

public class GetUserBookingsUseCase(IBookingRepository bookingRepository) : IGetUserBookingsUseCase
{
    public async Task<Result<List<BookingItemResponse>>> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var bookings = await bookingRepository.GetByUserIdAsync(userId, cancellationToken);

            var items = bookings.Select(b => new BookingItemResponse(
                BookingId: b.Id,
                RoomId: b.RoomId,
                UserId: b.UserId,
                TermName: b.Term.TermName,
                StartDate: b.Term.StartDate,
                EndDate: b.Term.EndDate,
                NumberOfMonths: b.Term.NumberOfMonths,
                PricePerMonth: b.PricePerMonth,
                BasePrice: b.BasePrice,
                TotalPrice: b.TotalPrice,
                Status: b.Status.ToString(),
                CreatedAt: b.CreatedAt,
                UpdatedAt: b.UpdatedAt,
                Fees: b.Fees.Select(f => new BookingFeeResponse(
                    Id: f.Id,
                    FeeName: f.FeeName,
                    Amount: f.Amount,
                    IsRefundable: f.IsRefundable
                )).ToList()
            )).ToList();

            return Result<List<BookingItemResponse>>.Success(items);
        }
        catch (Exception ex)
        {
            return Result<List<BookingItemResponse>>.Failure(ex.Message);
        }
    }
}
