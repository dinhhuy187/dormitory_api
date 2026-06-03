using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Auth;
using Billing.API.Infrastructure.Database;
using Billing.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Invoices;

public static class GetMyInvoices
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/billing/invoices/me", async (
                    int? year,
                    short? month,
                    InvoiceStatus? status,
                    int? page,
                    int? pageSize,
                    Handler handler,
                    HttpContext httpContext,
                    CancellationToken ct) =>
                {
                    if (!CurrentUser.TryGetUserId(httpContext.User, out var studentId))
                    {
                        return Results.Unauthorized();
                    }

                    if (!TryGetBearerToken(httpContext, out var accessToken))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await handler.ExecuteAsync(
                        new Query(year, month, status, page ?? 1, pageSize ?? 20),
                        studentId,
                        accessToken,
                        ct);

                    return Results.Ok(new ApiResponse<IReadOnlyList<InvoiceListItemResponse>>(
                        result.Items,
                        new PaginationMetadata(result.TotalItems, result.PageSize, result.Page)));
                })
                .WithTags("Billing - Invoices")
                .WithName("GetMyInvoices")
                .WithDescription("Required role: Student. Lists invoices for the authenticated student. Booking registration invoices are matched by StudentId; monthly utility invoices are matched by RoomId from the student's active or confirmed bookings resolved through BookingService. Room name is resolved from RoomService for the returned page. Optional filters are year, month, and status. Status enum values are Unpaid, WaitForConfirm, Paid, and Canceled. Response items include invoice type metadata, booking id when present, room name, room location snapshot, term name, due date, and description from Billing. Pagination uses page and pageSize; defaults are page=1 and pageSize=20.")
                .RequireAuthorization(policy => policy.RequireRole("Student"))
                .Produces<IReadOnlyList<InvoiceListItemResponse>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized);
        }

        private static bool TryGetBearerToken(HttpContext httpContext, out string accessToken)
        {
            accessToken = string.Empty;
            var authorization = httpContext.Request.Headers.Authorization.ToString();
            const string bearerPrefix = "Bearer ";

            if (string.IsNullOrWhiteSpace(authorization) ||
                !authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            accessToken = authorization[bearerPrefix.Length..].Trim();
            return !string.IsNullOrWhiteSpace(accessToken);
        }
    }

    public sealed record Query(int? Year, short? Month, InvoiceStatus? Status, int Page, int PageSize);

    public sealed record PagedResult(IReadOnlyList<InvoiceListItemResponse> Items, int TotalItems, int Page, int PageSize);

    public sealed class Handler(
        BillingDbContext dbContext,
        IBookingContractClient bookingContractClient,
        IRoomBillingClient roomBillingClient)
    {
        public async Task<PagedResult> ExecuteAsync(
            Query query,
            Guid studentId,
            string accessToken,
            CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var eligibleRoomIds = await GetEligibleRoomIdsAsync(accessToken, cancellationToken);

            var invoicesQuery = dbContext.Invoices
                .AsNoTracking()
                .Where(invoice =>
                    (invoice.InvoiceType == InvoiceType.BookingRegistration && invoice.StudentId == studentId) ||
                    (invoice.InvoiceType == InvoiceType.MonthlyUtility && eligibleRoomIds.Contains(invoice.RoomId)));

            if (query.Year.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(invoice => invoice.BillingYear == query.Year.Value);
            }

            if (query.Month.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(invoice => invoice.BillingMonth == query.Month.Value);
            }

            if (query.Status.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(invoice => invoice.Status == query.Status.Value);
            }

            var totalItems = await invoicesQuery.CountAsync(cancellationToken);
            var invoices = await invoicesQuery
                .OrderByDescending(invoice => invoice.BillingYear)
                .ThenByDescending(invoice => invoice.BillingMonth)
                .ThenByDescending(invoice => invoice.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            var roomNames = await GetRoomNamesAsync(invoices.Select(invoice => invoice.RoomId), cancellationToken);

            return new PagedResult(
                invoices
                    .Select(invoice => InvoiceResponseMapper.ToListItem(
                        invoice,
                        roomNames.GetValueOrDefault(invoice.RoomId)))
                    .ToList(),
                totalItems,
                page,
                pageSize);
        }

        private async Task<Dictionary<Guid, string?>> GetRoomNamesAsync(
            IEnumerable<Guid> roomIds,
            CancellationToken cancellationToken)
        {
            var roomNames = new Dictionary<Guid, string?>();

            foreach (var roomId in roomIds.Distinct())
            {
                var room = await roomBillingClient.GetRoomBillingInfoAsync(roomId, cancellationToken);
                roomNames[roomId] = room?.RoomNumber;
            }

            return roomNames;
        }

        private async Task<List<Guid>> GetEligibleRoomIdsAsync(string accessToken, CancellationToken cancellationToken)
        {
            var bookings = await bookingContractClient.GetMyBookingsAsync(accessToken, cancellationToken);
            return bookings
                .Where(booking => IsEligibleRoomBooking(booking.Status))
                .Select(booking => booking.RoomId)
                .Distinct()
                .ToList();
        }

        private static bool IsEligibleRoomBooking(string status)
        {
            return status.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                   status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase);
        }
    }
}
