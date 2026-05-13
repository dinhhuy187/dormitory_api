using BookingService.Application.Common.Models;
using BookingService.Application.UseCases.Bookings.Commands.ConfirmBooking;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace BookingService.Infrastructure.EventHandlers.Internal;

public class ConfirmBookingCommandConsumer(IConfirmBookingUseCase useCase, ILogger<ConfirmBookingCommandConsumer> logger) : IConsumer<ConfirmBookingCommand>
{
    public async Task Consume(ConsumeContext<ConfirmBookingCommand> context)
    {
        logger.LogInformation("Nhận lệnh XÁC NHẬN Booking từ Saga: {BookingId}", context.Message.BookingId);

        var result = await useCase.ExecuteAsync(context.Message.BookingId, context.CancellationToken);

        if (!result.IsSuccess)
        {
            logger.LogError("Lỗi khi xác nhận Booking {BookingId}: {Error}", context.Message.BookingId, result.ErrorMessage);
            
            // QUAN TRỌNG: Phải ném Exception ra ngoài. 
            // Nếu bạn chỉ return bình thường, MassTransit sẽ tưởng là chạy thành công và xóa tin nhắn đi.
            // Bằng cách ném Exception, MassTransit sẽ biết là có lỗi, tự động thử lại (Retry) 
            // hoặc đẩy tin nhắn vào hàng đợi lỗi (Dead Letter Queue) để Admin vào xem lại.
            throw new Exception($"Failed to confirm booking: {result.ErrorMessage}"); 
        }

        logger.LogInformation("Đã xác nhận thành công Booking {BookingId}. Giao dịch hoàn tất!", context.Message.BookingId);
    }
}