using BookingService.Application.Abtractions.Data;
using BookingService.Application.Common;
using BookingService.Application.Common.Models;
using BookingService.Domain.Enums;
using BookingService.Domain.Repositories;
using BookingService.Domain.SeedWork;

namespace BookingService.Application.UseCases.Bookings.Commands.CheckoutBooking;

public sealed class CheckoutBookingUseCase(
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork) : ICheckoutBookingUseCase
{
    public async Task<Result<CheckoutBookingResponse>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var booking = await bookingRepository.GetCheckoutCandidateBookingByUserIdAsync(userId, cancellationToken);
            if (booking is null)
            {
                return Result<CheckoutBookingResponse>.Failure(
                    $"Khong tim thay booking dang Active hoac da Completed cho user: {userId}");
            }

            if (booking.Status == BookingStatus.Active)
            {
                booking.CheckOut();
            }
            else
            {
                booking.RequestCheckoutReleaseRetry();
            }

            bookingRepository.Update(booking);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CheckoutBookingResponse>.Success(new CheckoutBookingResponse(
                booking.Id,
                booking.UserId,
                booking.RoomId,
                booking.Status.ToString(),
                booking.UpdatedAt));
        }
        catch (DomainException ex)
        {
            return Result<CheckoutBookingResponse>.Failure(ex.Message);
        }
    }
}
