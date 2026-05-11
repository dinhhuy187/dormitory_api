using BookingService.Application.Abtractions.Data;
using BookingService.Application.Common;
using BookingService.Application.Common.Models;
using BookingService.Domain.Repositories;
using BookingService.Domain.SeedWork;

namespace BookingService.Application.UseCases.Bookings.Commands.ConfirmBooking;

public class ConfirmBookingUseCase(IBookingRepository bookingRepository, IUnitOfWork unitOfWork) : IConfirmBookingUseCase
{
    public async Task<Result<bool>> ExecuteAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Lấy dữ liệu từ Database
            var booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);

            if (booking == null)
            {
                return Result<bool>.Failure($"Không tìm thấy đơn đặt phòng với mã: {bookingId}");
            }

            // 2. Gọi Core Domain để thực thi logic xác nhận
            booking.Confirm();

            // 3. Đánh dấu Entity bị thay đổi (Với EF Core thường không cần Update nếu đã Tracking, 
            // nhưng gọi Update để đồng nhất interface)
            bookingRepository.Update(booking);

            // 4. Lưu xuống Database (Transaction an toàn)
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (DomainException ex)
        {
            // Bắt lỗi nếu Saga vô tình gửi lệnh Confirm cho 1 đơn đã bị Canceled
            return Result<bool>.Failure(ex.Message);
        }
    }
}