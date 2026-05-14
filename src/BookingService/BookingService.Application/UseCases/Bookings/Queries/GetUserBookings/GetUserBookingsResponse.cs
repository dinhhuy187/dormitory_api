namespace BookingService.Application.UseCases.Bookings.Queries.GetUserBookings;

public record BookingFeeResponse(Guid Id, string FeeName, decimal Amount, bool IsRefundable);

public record BookingItemResponse(
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
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<BookingFeeResponse> Fees
);
