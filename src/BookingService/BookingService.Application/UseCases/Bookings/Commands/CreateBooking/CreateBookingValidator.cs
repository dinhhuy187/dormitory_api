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
        .MaximumLength(50).WithMessage("Tên kỳ học không hợp lệ");
    }
}