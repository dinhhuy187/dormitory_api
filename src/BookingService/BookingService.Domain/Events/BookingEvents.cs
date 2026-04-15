using BookingService.Domain.SeedWork;

namespace BookingService.Domain.Events;

public record BookingCreatedDomainEvent(Guid BookingId, Guid RoomId, Guid UserId) : IDomainEvent;
public record BookingConfirmedDomainEvent(Guid BookingId, Guid RoomId) : IDomainEvent;
public record BookingCanceledDomainEvent(Guid BookingId, Guid RoomId) : IDomainEvent;
public record StudentCheckedInDomainEvent(Guid BookingId, Guid RoomId, Guid UserId) : IDomainEvent;
public record StudentCheckedOutDomainEvent(Guid BookingId, Guid RoomId) : IDomainEvent;
