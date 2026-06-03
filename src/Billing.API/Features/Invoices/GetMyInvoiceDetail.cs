using Billing.API.Infrastructure.Auth;
using Billing.API.Infrastructure.Database;
using Billing.API.Infrastructure.Services;
using Billing.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Invoices;

public static class GetMyInvoiceDetail
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/billing/invoices/me/{invoiceId:guid}", async (
                    Guid invoiceId,
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

                    var response = await handler.ExecuteAsync(invoiceId, studentId, accessToken, ct);
                    return Results.Ok(new ApiResponse<InvoiceDetailResponse>(response));
                })
                .WithTags("Billing - Invoices")
                .WithName("GetMyInvoiceDetail")
                .WithDescription("Required role: Student. Gets full invoice detail for the authenticated student only. Booking registration invoices are matched by StudentId; monthly utility invoices are matched by RoomId from the student's active or confirmed bookings resolved through BookingService. Status values are Unpaid, WaitForConfirm, Paid, and Canceled. Response includes invoice type metadata, room name and location snapshot, current building bank account from RoomService when the room still exists, old/new meter indices, tier snapshots, surcharges, totals, payment status, and contract template snapshot id.")
                .RequireAuthorization(policy => policy.RequireRole("Student"))
                .Produces<InvoiceDetailResponse>(StatusCodes.Status200OK)
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

    public sealed class Handler(
        BillingDbContext dbContext,
        IRoomBillingClient roomBillingClient,
        IBookingContractClient bookingContractClient)
    {
        public async Task<InvoiceDetailResponse> ExecuteAsync(
            Guid invoiceId,
            Guid studentId,
            string accessToken,
            CancellationToken cancellationToken)
        {
            var invoice = await dbContext.Invoices
                .AsNoTracking()
                .Include(invoice => invoice.Surcharges)
                .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken)
                ?? throw new ApiException("Invoice not found.", StatusCodes.Status404NotFound);

            if (!await CanAccessInvoiceAsync(invoice, studentId, accessToken, cancellationToken))
            {
                throw new ApiException("Invoice not found.", StatusCodes.Status404NotFound);
            }

            var room = await roomBillingClient.GetRoomBillingInfoAsync(invoice.RoomId, cancellationToken);
            var buildingBankAccount = room is null
                ? null
                : new BuildingBankAccountResponse(room.BankCode, room.AccountNumber, room.AccountName);

            return InvoiceResponseMapper.ToDetail(invoice, buildingBankAccount, room?.RoomNumber);
        }

        private async Task<bool> CanAccessInvoiceAsync(
            Domain.Entities.Invoice invoice,
            Guid studentId,
            string accessToken,
            CancellationToken cancellationToken)
        {
            if (invoice.InvoiceType == InvoiceType.BookingRegistration)
            {
                return invoice.StudentId == studentId;
            }

            if (invoice.InvoiceType != InvoiceType.MonthlyUtility)
            {
                return false;
            }

            var bookings = await bookingContractClient.GetMyBookingsAsync(accessToken, cancellationToken);
            return bookings.Any(booking =>
                booking.RoomId == invoice.RoomId &&
                IsEligibleRoomBooking(booking.Status));
        }

        private static bool IsEligibleRoomBooking(string status)
        {
            return status.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                   status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase);
        }
    }
}
