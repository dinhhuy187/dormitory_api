namespace Billing.API.Infrastructure.Services;

public interface IBookingContractClient
{
    Task<IReadOnlyList<BookingContractInfo>> GetMyBookingsAsync(
        string accessToken,
        CancellationToken cancellationToken);
}

public sealed record BookingContractInfo(
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
    IReadOnlyList<BookingContractFee> Fees);

public sealed record BookingContractFee(
    Guid Id,
    string FeeName,
    decimal Amount,
    bool IsRefundable);
