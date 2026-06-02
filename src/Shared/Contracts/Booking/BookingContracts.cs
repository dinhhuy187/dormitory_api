namespace Shared.Contracts.Booking;

public sealed record ReserveRoomCapacityCommand(
    Guid BookingId,
    Guid RoomId,
    Guid StudentId,
    DateTime RequestedAt)
{
    public ReserveRoomCapacityCommand() : this(default, default, default, default) { }
}

public sealed record RoomCapacityReservedEvent(
    Guid BookingId,
    Guid RoomId,
    Guid StudentId,
    int OccupiedCount,
    int Capacity,
    DateTime ReservedAt);

public sealed record RoomCapacityReservationFailedEvent(
    Guid BookingId,
    Guid RoomId,
    Guid StudentId,
    string Reason,
    DateTime FailedAt);

public sealed record ReleaseRoomCapacityCommand(
    Guid BookingId,
    Guid RoomId,
    Guid StudentId,
    string Reason,
    DateTime RequestedAt)
{
    public ReleaseRoomCapacityCommand() : this(default, default, default, string.Empty, default) { }
}

public sealed record RoomCapacityReleasedEvent(
    Guid BookingId,
    Guid RoomId,
    Guid StudentId,
    int OccupiedCount,
    int Capacity,
    DateTime ReleasedAt);

public sealed record CreateInvoiceCommand(
    Guid BookingId,
    Guid StudentId,
    Guid RoomId,
    string TermName,
    DateTime StartDate,
    DateTime EndDate,
    int NumberOfMonths,
    decimal PricePerMonth,
    decimal BasePrice,
    decimal TotalPrice,
    DateTime BookingCreatedAt,
    DateTime PaymentDueAt,
    IReadOnlyList<BookingInvoiceFeeLine> Fees)
{
    public CreateInvoiceCommand() : this(default, default, default, string.Empty, default, default, default, default, default, default, default, default, Array.Empty<BookingInvoiceFeeLine>()) { }
}

public sealed record BookingInvoiceFeeLine(
    string FeeName,
    decimal Amount,
    bool IsRefundable);

public sealed record BookingInvoiceCreatedEvent(
    Guid BookingId,
    Guid InvoiceId,
    Guid StudentId,
    Guid RoomId,
    decimal TotalAmount,
    DateTime DueAt,
    DateTime CreatedAt);

public sealed record CancelBookingInvoiceCommand(
    Guid BookingId,
    string Reason,
    DateTime RequestedAt)
{
    public CancelBookingInvoiceCommand() : this(default, string.Empty, default) { }
}

public sealed record BookingInvoiceCanceledEvent(
    Guid BookingId,
    Guid InvoiceId,
    string Reason,
    DateTime CanceledAt);

public sealed record PaymentSucceededIntegrationEvent(
    Guid BookingId,
    Guid? InvoiceId,
    DateTime PaidAt);

public sealed record PaymentFailedIntegrationEvent(
    Guid BookingId,
    Guid? InvoiceId,
    string Reason,
    DateTime FailedAt);

public sealed record BookingPaymentExpiredEvent(
    Guid BookingId,
    DateTime ExpiredAt,
    string Reason);
