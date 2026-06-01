using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Auth;
using Billing.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Reports;

public static class GetMyUtilityHistory
{
    public sealed record UtilityHistoryItemResponse(
        short Month,
        int Year,
        decimal ElectricityAmount,
        decimal WaterAmount);

    public sealed record Query(int? Year, int Limit);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/billing/reports/me/utility-history", async (
                    int? year,
                    int? limit,
                    Handler handler,
                    HttpContext httpContext,
                    CancellationToken ct) =>
                {
                    if (!CurrentUser.TryGetUserId(httpContext.User, out var studentId))
                    {
                        return Results.Unauthorized();
                    }

                    var response = await handler.ExecuteAsync(new Query(year, limit ?? 12), studentId, ct);
                    return Results.Ok(new ApiResponse<IReadOnlyList<UtilityHistoryItemResponse>>(response));
                })
                .WithTags("Billing - Reports")
                .WithName("GetMyUtilityHistory")
                .WithDescription("Required role: Student. Returns utility cost history for dashboard charts. The student id is taken from the authenticated JWT user id. Canceled invoices are excluded. When year is provided, available months in that year are returned; otherwise the latest limit periods are returned, with limit clamped from 1 to 36.")
                .RequireAuthorization(policy => policy.RequireRole("Student"))
                .Produces<IReadOnlyList<UtilityHistoryItemResponse>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized);
        }
    }

    public sealed class Handler(BillingDbContext dbContext)
    {
        public async Task<IReadOnlyList<UtilityHistoryItemResponse>> ExecuteAsync(
            Query query,
            Guid studentId,
            CancellationToken cancellationToken)
        {
            if (query.Year is < 2020 or > 2100)
            {
                throw new ApiException("year must be between 2020 and 2100.", StatusCodes.Status400BadRequest);
            }

            var invoicesQuery = dbContext.Invoices
                .AsNoTracking()
                .Where(invoice => invoice.StudentId == studentId &&
                                  invoice.InvoiceType == InvoiceType.MonthlyUtility &&
                                  invoice.Status != InvoiceStatus.Canceled);

            if (query.Year.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(invoice => invoice.BillingYear == query.Year.Value);
            }

            var groupedQuery = invoicesQuery
                .GroupBy(invoice => new { invoice.BillingYear, invoice.BillingMonth });

            if (query.Year.HasValue)
            {
                return await groupedQuery
                    .OrderBy(group => group.Key.BillingYear)
                    .ThenBy(group => group.Key.BillingMonth)
                    .Select(group => new UtilityHistoryItemResponse(
                        group.Key.BillingMonth,
                        group.Key.BillingYear,
                        group.Sum(invoice => invoice.ElectricityAmount),
                        group.Sum(invoice => invoice.WaterAmount)))
                    .ToListAsync(cancellationToken);
            }

            var limit = Math.Clamp(query.Limit, 1, 36);
            var latestItems = await groupedQuery
                .OrderByDescending(group => group.Key.BillingYear)
                .ThenByDescending(group => group.Key.BillingMonth)
                .Take(limit)
                .Select(group => new UtilityHistoryItemResponse(
                    group.Key.BillingMonth,
                    group.Key.BillingYear,
                    group.Sum(invoice => invoice.ElectricityAmount),
                    group.Sum(invoice => invoice.WaterAmount)))
                .ToListAsync(cancellationToken);

            return latestItems
                .OrderBy(item => item.Year)
                .ThenBy(item => item.Month)
                .ToList();
        }
    }
}
