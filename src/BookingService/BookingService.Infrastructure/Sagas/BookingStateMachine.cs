using BookingService.Application.Contracts.IntegrationCommands;
using BookingService.Application.Contracts.IntegrationEvents;
using BookingService.Application.UseCases.Bookings.Commands.CancelBooking;
using BookingService.Application.UseCases.Bookings.Commands.ConfirmBooking;
using BookingService.Domain.Events;
using MassTransit;

namespace BookingService.Infrastructure.Sagas;

public class BookingStateMachine : MassTransitStateMachine<BookingSagaState>
{
    // Các trạng thái của luồng Saga
    public State AwaitingPayment { get; private set; }
    public State PaymentCompleted { get; private set; }

    // Các sự kiện lắng nghe
    public Event<BookingCreatedDomainEvent> BookingCreatedEvent { get; private set; } 
    public Event<PaymentSucceededIntegrationEvent> PaymentSucceededEvent { get; private set; }
    public Event<PaymentFailedIntegrationEvent> PaymentFailedEvent { get; private set; }

    public BookingStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // Định nghĩa khóa tương quan (Correlation) để gom các event về chung 1 luồng
        Event(() => BookingCreatedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => PaymentSucceededEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => PaymentFailedEvent, x => x.CorrelateById(context => context.Message.BookingId));

        // BƯỚC 1: Bắt đầu Saga khi Booking được tạo
        Initially(
            When(BookingCreatedEvent)
                .Then(context =>
                {
                    context.Saga.UserId = context.Message.UserId;
                    context.Saga.RoomId = context.Message.RoomId;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                })
                .TransitionTo(AwaitingPayment)
                // Gọi sang Payment Service để tạo hóa đơn
                .PublishAsync(context => context.Init<CreateInvoiceCommand>(new
                {
                    BookingId = context.Saga.CorrelationId,
                    UserId = context.Saga.UserId
                }))
        );

        // BƯỚC 2: Xử lý trong lúc đang chờ thanh toán
        During(AwaitingPayment,
            // 2A. Nếu thanh toán thành công
            When(PaymentSucceededEvent)
                .TransitionTo(PaymentCompleted)
                // Gọi ngược lại Booking UseCase để Confirm
                .PublishAsync(context => context.Init<ConfirmBookingCommand>(new { BookingId = context.Saga.CorrelationId }))
                .Finalize(), // Kết thúc luồng Saga thành công

            // 2B. Nếu thanh toán thất bại (Logic Compensation - Hoàn tác)
            When(PaymentFailedEvent)
                // Báo cho Room Service trả lại chỗ trống
                .PublishAsync(context => context.Init<ReleaseRoomCapacityCommand>(new { RoomId = context.Saga.RoomId }))
                // Đánh dấu Booking bị hủy
                .PublishAsync(context => context.Init<CancelBookingCommand>(new { BookingId = context.Saga.CorrelationId }))
                .Finalize() // Kết thúc luồng Saga thất bại
        );

        SetCompletedWhenFinalized(); // Tự xóa dòng trong DB khi Saga kết thúc để đỡ nặng máy
    }
}