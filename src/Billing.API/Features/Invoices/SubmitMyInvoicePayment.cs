using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Auth;
using Billing.API.Infrastructure.Database;
using Billing.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Invoices;

public static class SubmitMyInvoicePayment
{
    public sealed record Response(
        Guid InvoiceId,
        string Status,
        DateTime SubmittedAt,
        Guid UpdatedByUserId,
        decimal TotalAmount);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/billing/invoices/me/{invoiceId:guid}/payment-confirmation", async (
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
                    return Results.Ok(new ApiResponse<Response>(response));
                })
                .WithTags("Billing - Invoices")
                .WithName("SubmitMyInvoicePayment")
                .WithDescription("Required role: Student. Submits an invoice payment for staff confirmation. Booking registration invoices are matched by StudentId; monthly utility invoices are matched by RoomId from the student's active or confirmed bookings resolved through BookingService. The only allowed state transition is Unpaid to WaitForConfirm. PaidAt is not set until staff marks the invoice as Paid. Status values are Unpaid, WaitForConfirm, Paid, and Canceled.")
                .RequireAuthorization(policy => policy.RequireRole("Student"))
                .Produces<Response>(StatusCodes.Status200OK)
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

    public sealed class Handler(BillingDbContext dbContext, IBookingContractClient bookingContractClient)
    {
        public async Task<Response> ExecuteAsync(
            Guid invoiceId,
            Guid studentId,
            string accessToken,
            CancellationToken cancellationToken)
        {
            var invoice = await dbContext.Invoices
                .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken)
                ?? throw new ApiException("Invoice not found.", StatusCodes.Status404NotFound);

            if (!await CanAccessInvoiceAsync(invoice, studentId, accessToken, cancellationToken))
            {
                throw new ApiException("Invoice not found.", StatusCodes.Status404NotFound);
            }

            if (invoice.Status == InvoiceStatus.WaitForConfirm)
            {
                throw new ApiException("Invoice payment is already waiting for confirmation.", StatusCodes.Status409Conflict);
            }

            if (invoice.Status == InvoiceStatus.Paid)
            {
                throw new ApiException("Invoice has already been paid.", StatusCodes.Status409Conflict);
            }

            if (invoice.Status == InvoiceStatus.Canceled)
            {
                throw new ApiException("Canceled invoices cannot be submitted for payment confirmation.", StatusCodes.Status409Conflict);
            }

            var now = DateTime.UtcNow;
            invoice.Status = InvoiceStatus.WaitForConfirm;
            invoice.UpdatedByUserId = studentId;
            invoice.UpdatedAt = now;

            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response(invoice.Id, invoice.Status.ToString(), now, studentId, invoice.TotalAmount);
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
