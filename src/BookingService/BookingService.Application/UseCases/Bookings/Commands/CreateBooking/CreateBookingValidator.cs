using FluentValidation;

namespace BookingService.Application.UseCases.Bookings.Commands.CreateBooking;

public class CreateBookingValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty().WithMessage("Mã phòng không được để trống.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Mã sinh viên không được để trống");
        RuleFor(x => x.TermName)
        .NotEmpty().WithMessage("Vui lòng chọn năm học / kỳ học đăng ký.")
        .Equal("HK2_2025_2026").WithMessage("Tên kỳ học chỉ được là HK2_2025_2026.");
    }
}
