using Billing.API.Domain.Entities;
using Billing.API.Infrastructure.Auth;
using Billing.API.Infrastructure.Database;
using Billing.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using Shared.Services;

namespace Billing.API.Features.Contracts;

public static class GetMyContract
{
    public sealed record Response(
        StudentInfoResponse Student,
        RoomInfoResponse? Room,
        BookingInfoResponse? Booking,
        Guid? LatestInvoiceId,
        Guid ContractTemplateId,
        Guid? RoomTypeId,
        string Code,
        string Name,
        int Version,
        string Content,
        DateOnly EffectiveFrom,
        DateOnly? EffectiveTo,
        string Source);

    public sealed record StudentInfoResponse(
        Guid StudentId,
        string? FullName);

    public sealed record RoomInfoResponse(
        Guid RoomId,
        string RoomNumber,
        string? BuildingCode,
        int? Floor,
        Guid? RoomTypeId,
        string? RoomTypeName,
        int? Capacity,
        string? Status);

    public sealed record BookingInfoResponse(
        Guid BookingId,
        string TermName,
        DateTime StartDate,
        DateTime EndDate,
        int NumberOfMonths,
        decimal MonthlyRent,
        decimal BaseRentTotal,
        decimal BookingFeeTotal,
        decimal TotalBookingAmount,
        string Status,
        IReadOnlyList<BookingFeeResponse> Fees);

    public sealed record BookingFeeResponse(
        Guid Id,
        string FeeName,
        decimal Amount,
        bool IsRefundable);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/billing/contracts/me", async (
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

                    var response = await handler.ExecuteAsync(studentId, accessToken, ct);
                    return Results.Ok(new ApiResponse<Response>(response));
                })
                .WithTags("Billing - Contracts")
                .WithName("GetMyContract")
                .WithDescription("Required role: Student. Gets contract information for the authenticated student. StudentId comes from JWT. Student full name is resolved from Profile service. Room and booking information are resolved from RoomService and BookingService; booking fees are registration fees and monthly rent is BookingService PricePerMonth. Source values are LatestInvoice, BookingRoomType, and ActiveTemplate.")
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

    public sealed class Handler(
        BillingDbContext dbContext,
        IBookingContractClient bookingContractClient,
        IRoomBillingClient roomBillingClient,
        IProfileService profileService)
    {
        public async Task<Response> ExecuteAsync(
            Guid studentId,
            string accessToken,
            CancellationToken cancellationToken)
        {
            var latestInvoice = await dbContext.Invoices
                .AsNoTracking()
                .Include(invoice => invoice.ContractTemplate)
                .Where(invoice => invoice.StudentId == studentId)
                .OrderByDescending(invoice => invoice.BillingYear)
                .ThenByDescending(invoice => invoice.BillingMonth)
                .ThenByDescending(invoice => invoice.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var fullName = await GetStudentFullNameAsync(studentId, accessToken, cancellationToken);
            var bookings = await bookingContractClient.GetMyBookingsAsync(accessToken, cancellationToken);
            var selectedBooking = SelectRelevantBooking(bookings, latestInvoice?.RoomId);

            var roomId = latestInvoice?.RoomId ?? selectedBooking?.RoomId;
            var room = roomId.HasValue
                ? await GetRoomInfoAsync(roomId.Value, cancellationToken)
                : null;

            var effectiveDate = latestInvoice is null
                ? DateOnly.FromDateTime(DateTime.UtcNow)
                : new DateOnly(latestInvoice.BillingYear, latestInvoice.BillingMonth, 1);

            var (template, source) = await SelectContractTemplateAsync(
                latestInvoice?.ContractTemplate,
                room?.RoomTypeId,
                effectiveDate,
                cancellationToken);

            if (template is null)
            {
                throw new ApiException("Contract template not found.", StatusCodes.Status404NotFound);
            }

            return new Response(
                new StudentInfoResponse(studentId, fullName),
                room,
                selectedBooking is null ? null : MapBooking(selectedBooking),
                latestInvoice?.Id,
                template.Id,
                template.RoomTypeId,
                template.Code,
                template.Name,
                template.Version,
                template.Content,
                template.EffectiveFrom,
                template.EffectiveTo,
                source);
        }

        private async Task<string?> GetStudentFullNameAsync(
            Guid studentId,
            string accessToken,
            CancellationToken cancellationToken)
        {
            var profiles = await profileService.GetProfilesAsync(
                [studentId.ToString()],
                accessToken,
                cancellationToken);

            return profiles.TryGetValue(studentId.ToString(), out var profile)
                ? profile.FullName
                : null;
        }

        private static BookingContractInfo? SelectRelevantBooking(
            IReadOnlyList<BookingContractInfo> bookings,
            Guid? latestInvoiceRoomId)
        {
            if (bookings.Count == 0)
            {
                return null;
            }

            if (latestInvoiceRoomId.HasValue)
            {
                var invoiceRoomBooking = bookings
                    .Where(booking => booking.RoomId == latestInvoiceRoomId.Value)
                    .OrderByDescending(booking => booking.CreatedAt)
                    .FirstOrDefault();

                if (invoiceRoomBooking is not null)
                {
                    return invoiceRoomBooking;
                }
            }

            return bookings
                .OrderBy(booking => GetStatusPriority(booking.Status))
                .ThenByDescending(booking => booking.CreatedAt)
                .FirstOrDefault();
        }

        private static int GetStatusPriority(string status)
        {
            if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return 2;
        }

        private async Task<RoomInfoResponse?> GetRoomInfoAsync(Guid roomId, CancellationToken cancellationToken)
        {
            try
            {
                var roomInfo = await roomBillingClient.GetRoomBillingInfoAsync(roomId, cancellationToken);
                return roomInfo is null
                    ? null
                    : new RoomInfoResponse(
                        roomInfo.RoomId,
                        roomInfo.RoomNumber,
                        roomInfo.BuildingCode,
                        roomInfo.Floor,
                        roomInfo.RoomTypeId,
                        roomInfo.RoomTypeName,
                        roomInfo.Capacity,
                        roomInfo.Status);
            }
            catch (ApiException)
            {
                return null;
            }
        }

        private async Task<(ContractTemplate? Template, string Source)> SelectContractTemplateAsync(
            ContractTemplate? invoiceTemplate,
            Guid? roomTypeId,
            DateOnly effectiveDate,
            CancellationToken cancellationToken)
        {
            if (invoiceTemplate is not null)
            {
                return (invoiceTemplate, "LatestInvoice");
            }

            if (roomTypeId.HasValue)
            {
                var roomTypeTemplate = await GetActiveTemplateAsync(roomTypeId.Value, effectiveDate, cancellationToken);
                if (roomTypeTemplate is not null)
                {
                    return (roomTypeTemplate, "BookingRoomType");
                }
            }

            return (await GetActiveTemplateAsync(null, effectiveDate, cancellationToken), "ActiveTemplate");
        }

        private Task<ContractTemplate?> GetActiveTemplateAsync(
            Guid? roomTypeId,
            DateOnly effectiveDate,
            CancellationToken cancellationToken)
        {
            return dbContext.ContractTemplates
                .AsNoTracking()
                .Where(contractTemplate => contractTemplate.IsActive &&
                                           contractTemplate.RoomTypeId == roomTypeId &&
                                           contractTemplate.EffectiveFrom <= effectiveDate &&
                                           (contractTemplate.EffectiveTo == null ||
                                            contractTemplate.EffectiveTo >= effectiveDate))
                .OrderByDescending(contractTemplate => contractTemplate.EffectiveFrom)
                .ThenByDescending(contractTemplate => contractTemplate.Version)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static BookingInfoResponse MapBooking(BookingContractInfo booking)
        {
            var fees = booking.Fees
                .Select(fee => new BookingFeeResponse(
                    fee.Id,
                    fee.FeeName,
                    fee.Amount,
                    fee.IsRefundable))
                .ToList();

            return new BookingInfoResponse(
                booking.BookingId,
                booking.TermName,
                booking.StartDate,
                booking.EndDate,
                booking.NumberOfMonths,
                booking.PricePerMonth,
                booking.BasePrice,
                fees.Sum(fee => fee.Amount),
                booking.TotalPrice,
                booking.Status,
                fees);
        }
    }
}
