using BookingService.Application.UseCases.Bookings.Commands.CancelBooking;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace BookingService.Infrastructure.EventHandlers.Internal;

public class CancelBookingCommandConsumer(ICancelBookingUseCase useCase, ILogger<CancelBookingCommandConsumer> logger) : IConsumer<CancelBookingCommand>
{
    public async Task Consume(ConsumeContext<CancelBookingCommand> context)
    {
        logger.LogWarning("Nhận lệnh HỦY Booking từ Saga: {BookingId}", context.Message.BookingId);

        var result = await useCase.ExecuteAsync(context.Message, context.CancellationToken);

        if (!result.IsSuccess)
        {
            logger.LogError("Không thể hủy Booking {BookingId}. Lỗi: {Error}", context.Message.BookingId, result.ErrorMessage);
            
            // Ném lỗi để MassTransit biết và tự động Retry lại theo cấu hình
            throw new Exception($"Failed to cancel booking: {result.ErrorMessage}");
        }
        
        logger.LogInformation("Đã hủy thành công Booking {BookingId}.", context.Message.BookingId);
    }
}