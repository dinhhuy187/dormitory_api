using BookingService.Application.Abtractions.Data;
using BookingService.Application.Common;
using BookingService.Domain.Repositories;
using BookingService.Domain.SeedWork;

namespace BookingService.Application.UseCases.Bookings.Commands.CancelBooking;

public class CancelBookingUseCase(IBookingRepository bookingRepository, IUnitOfWork unitOfWork) : ICancelBookingUseCase
{
    public async Task<Result<bool>> ExecuteAsync(CancelBookingCommand request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Lấy đơn đặt phòng
            var booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

            if (booking == null)
            {
                return Result<bool>.Failure($"Không tìm thấy đơn đặt phòng: {request.BookingId}");
            }

            // 2. Gọi logic Hủy ở tầng Domain
            booking.Cancel();

            // 3. Đánh dấu theo dõi
            bookingRepository.Update(booking);

            // 4. Lưu xuống Database cùng với các Domain Event (nếu có) vào Outbox
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (DomainException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }
    }
}