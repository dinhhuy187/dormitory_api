using BookingService.Application.UseCases.Bookings.Commands.CancelBooking;
using BookingService.Application.UseCases.Bookings.Commands.ConfirmBooking;
using BookingService.Domain.Events;
using MassTransit;
using Shared.Contracts.Booking;
using System.Text.Json;

namespace BookingService.Infrastructure.Sagas;

public class BookingStateMachine : MassTransitStateMachine<BookingSagaState>
{
    public State ReservingRoom { get; private set; }
    public State AwaitingInvoice { get; private set; }
    public State AwaitingPayment { get; private set; }
    public State PaymentCompleted { get; private set; }

    public Event<BookingCreatedDomainEvent> BookingCreatedEvent { get; private set; }
    public Event<RoomCapacityReservedEvent> RoomCapacityReservedEvent { get; private set; }
    public Event<RoomCapacityReservationFailedEvent> RoomCapacityReservationFailedEvent { get; private set; }
    public Event<BookingInvoiceCreatedEvent> BookingInvoiceCreatedEvent { get; private set; }
    public Event<PaymentSucceededIntegrationEvent> PaymentSucceededEvent { get; private set; }
    public Event<PaymentFailedIntegrationEvent> PaymentFailedEvent { get; private set; }
    public Event<BookingPaymentExpiredEvent> BookingPaymentExpiredEvent { get; private set; }

    public BookingStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => BookingCreatedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => RoomCapacityReservedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => RoomCapacityReservationFailedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => BookingInvoiceCreatedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => PaymentSucceededEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => PaymentFailedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => BookingPaymentExpiredEvent, x => x.CorrelateById(context => context.Message.BookingId));

        Initially(
            When(BookingCreatedEvent)
                .Then(context =>
                {
                    context.Saga.UserId = context.Message.UserId;
                    context.Saga.RoomId = context.Message.RoomId;
                    context.Saga.TermName = context.Message.TermName;
                    context.Saga.StartDate = context.Message.StartDate;
                    context.Saga.EndDate = context.Message.EndDate;
                    context.Saga.NumberOfMonths = context.Message.NumberOfMonths;
                    context.Saga.PricePerMonth = context.Message.PricePerMonth;
                    context.Saga.BasePrice = context.Message.BasePrice;
                    context.Saga.TotalPrice = context.Message.TotalPrice;
                    context.Saga.CreatedAt = context.Message.CreatedAt;
                    context.Saga.PaymentDueAt = context.Message.PaymentDueAt;
                    var feeSnapshot = context.Message.Fees
                        .Select(f => new BookingInvoiceFeeSnapshot
                        {
                            FeeName = f.FeeName,
                            Amount = f.Amount,
                            IsRefundable = f.IsRefundable
                        })
                        .ToList();
                    context.Saga.FeesJson = JsonSerializer.Serialize(feeSnapshot);
                })
                .TransitionTo(ReservingRoom)
                .PublishAsync(context => context.Init<ReserveRoomCapacityCommand>(new
                {
                    BookingId = context.Saga.CorrelationId,
                    RoomId = context.Saga.RoomId,
                    StudentId = context.Saga.UserId,
                    RequestedAt = DateTime.UtcNow
                })));

        During(ReservingRoom,
            When(RoomCapacityReservedEvent)
                .TransitionTo(AwaitingInvoice)
                .PublishAsync(context => context.Init<CreateInvoiceCommand>(new
                {
                    BookingId = context.Saga.CorrelationId,
                    StudentId = context.Saga.UserId,
                    RoomId = context.Saga.RoomId,
                    TermName = context.Saga.TermName,
                    StartDate = context.Saga.StartDate,
                    EndDate = context.Saga.EndDate,
                    NumberOfMonths = context.Saga.NumberOfMonths,
                    PricePerMonth = context.Saga.PricePerMonth,
                    BasePrice = context.Saga.BasePrice,
                    TotalPrice = context.Saga.TotalPrice,
                    BookingCreatedAt = context.Saga.CreatedAt,
                    PaymentDueAt = context.Saga.PaymentDueAt,
                    Fees = DeserializeFees(context.Saga.FeesJson).Select(f => new BookingInvoiceFeeLine(
                        f.FeeName,
                        f.Amount,
                        f.IsRefundable)).ToArray()
                })),

            When(RoomCapacityReservationFailedEvent)
                .PublishAsync(context => context.Init<CancelBookingCommand>(new
                {
                    BookingId = context.Saga.CorrelationId
                }))
                .Finalize());

        During(AwaitingInvoice,
            When(BookingInvoiceCreatedEvent)
                .Then(context =>
                {
                    context.Saga.InvoiceId = context.Message.InvoiceId;
                })
                .TransitionTo(AwaitingPayment));

        During(AwaitingPayment,
            When(PaymentSucceededEvent)
                .TransitionTo(PaymentCompleted)
                .PublishAsync(context => context.Init<ConfirmBookingCommand>(new
                {
                    BookingId = context.Saga.CorrelationId
                }))
                .Finalize(),

            When(PaymentFailedEvent)
                .PublishPaymentFailureCompensation(context => context.Message.Reason, _ => DateTime.UtcNow)
                .Finalize(),

            When(BookingPaymentExpiredEvent)
                .PublishPaymentFailureCompensation(context => context.Message.Reason, context => context.Message.ExpiredAt)
                .Finalize());

        SetCompletedWhenFinalized();
    }

    private static IReadOnlyList<BookingInvoiceFeeSnapshot> DeserializeFees(string feesJson)
    {
        return JsonSerializer.Deserialize<BookingInvoiceFeeSnapshot[]>(feesJson) ?? [];
    }
}

public static class BookingStateMachineCompensationExtensions
{
    public static EventActivityBinder<BookingSagaState, TMessage> PublishPaymentFailureCompensation<TMessage>(
        this EventActivityBinder<BookingSagaState, TMessage> binder,
        Func<BehaviorContext<BookingSagaState, TMessage>, string> reasonFactory,
        Func<BehaviorContext<BookingSagaState, TMessage>, DateTime> requestedAtFactory)
        where TMessage : class
    {
        return binder
            .PublishAsync(context => context.Init<ReleaseRoomCapacityCommand>(new
            {
                BookingId = context.Saga.CorrelationId,
                RoomId = context.Saga.RoomId,
                StudentId = context.Saga.UserId,
                Reason = reasonFactory(context),
                RequestedAt = requestedAtFactory(context)
            }))
            .PublishAsync(context => context.Init<CancelBookingInvoiceCommand>(new
            {
                BookingId = context.Saga.CorrelationId,
                Reason = reasonFactory(context),
                RequestedAt = requestedAtFactory(context)
            }))
            .PublishAsync(context => context.Init<CancelBookingCommand>(new
            {
                BookingId = context.Saga.CorrelationId
            }));
    }
}
