using BookingService.Application.Abtractions.Data;
using BookingService.Application.Abtractions.Services;
using BookingService.Application.Common;
using BookingService.Application.Common.Models;
using BookingService.Domain.Entities;
using BookingService.Domain.Repositories;
using BookingService.Domain.SeedWork;
using BookingService.Domain.Services;

namespace BookingService.Application.UseCases.Bookings.Commands.CreateBooking;

public class CreateBookingUseCase(
    IBookingRepository bookingRepository,
    IAcademicTermRepository academicTermRepository,
    IUnitOfWork unitOfWork,
    IRoomPricingService roomPricingService,
    IBookingRulesChecker bookingRulesChecker,
    IRegistrationPeriodChecker registrationPeriodChecker) : ICreateBookingUseCase
{
    public async Task<Result<CreateBookingResponse>> ExecuteAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var term = await academicTermRepository.GetTermByNameAsync(request.TermName, cancellationToken);
            if (term == null)
            {    
                return Result<CreateBookingResponse>.Failure("Academic term not found.");
            }
            
            var pricePerMonth = await roomPricingService.GetMonthlyPriceAsync(request.RoomId, cancellationToken);
            if (pricePerMonth <= 0)
            {
                return Result<CreateBookingResponse>.Failure("Dữ liệu giá của phòng này đang bị lỗi, vui lòng liên hệ Ban quản lý..");
            }

            var booking = await Booking.CreateAsync(
                request.RoomId,
                request.UserId,
                term,
                pricePerMonth,
                bookingRulesChecker,
                registrationPeriodChecker,
                cancellationToken);
            
            // 4. Nghiệp vụ thực tế: Thêm các loại phụ phí cố định bắt buộc
            booking.AddFee("Phí làm hồ sơ lưu trú", 60000, isRefundable: false);
            booking.AddFee("Tiền thế chân tài sản", 100000, isRefundable: true);

            bookingRepository.Add(booking);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new CreateBookingResponse(
                BookingId: booking.Id,
                Status: booking.Status.ToString(),
                TotalPrice: booking.TotalPrice,
                Message: "Tạo đơn đặt phòng thành công. Vui lòng thanh toán trong vòng 48h."
            );
            return Result<CreateBookingResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<CreateBookingResponse>.Failure(ex.Message);
        }
    }
}