using BookingService.Domain.SeedWork;

namespace BookingService.Domain.Events;

public record BookingCreatedDomainEvent(
    Guid BookingId,
    Guid RoomId,
    Guid UserId,
    string TermName,
    DateTime StartDate,
    DateTime EndDate,
    int NumberOfMonths,
    decimal PricePerMonth,
    decimal BasePrice,
    decimal TotalPrice,
    DateTime CreatedAt,
    DateTime PaymentDueAt,
    IReadOnlyList<BookingCreatedFeeSnapshot> Fees) : IDomainEvent;

public record BookingCreatedFeeSnapshot(
    string FeeName,
    decimal Amount,
    bool IsRefundable);

public record BookingConfirmedDomainEvent(Guid BookingId, Guid RoomId) : IDomainEvent;
public record BookingCanceledDomainEvent(Guid BookingId, Guid RoomId) : IDomainEvent;
public record StudentCheckedInDomainEvent(Guid BookingId, Guid RoomId, Guid UserId) : IDomainEvent;
public record StudentCheckedOutDomainEvent(Guid BookingId, Guid RoomId, Guid UserId) : IDomainEvent;
