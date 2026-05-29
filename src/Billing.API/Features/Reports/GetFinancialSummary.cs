using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Reports;

public static class GetFinancialSummary
{
    public sealed record Response(
        string PeriodType,
        int Year,
        int? Month,
        int? Quarter,
        decimal TotalRevenue,
        decimal TotalOutstanding,
        int PaidInvoiceCount,
        int UnpaidInvoiceCount);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/billing/reports/financial-summary", async (
                    string periodType,
                    int year,
                    int? month,
                    int? quarter,
                    Handler handler,
                    CancellationToken ct) =>
                {
                    var response = await handler.ExecuteAsync(new Query(periodType, year, month, quarter), ct);
                    return Results.Ok(new ApiResponse<Response>(response));
                })
                .WithTags("Billing - Reports")
                .WithName("GetFinancialSummary")
                .WithDescription("Required role: Admin. Returns financial KPI summary. periodType accepts month, quarter, or year. For month, provide month 1-12. For quarter, provide quarter 1-4. TotalRevenue sums Paid invoices; TotalOutstanding sums Unpaid invoices.")
                .RequireAuthorization(policy => policy.RequireRole("Admin"))
                .Produces<Response>(StatusCodes.Status200OK);
        }
    }

    public sealed record Query(string PeriodType, int Year, int? Month, int? Quarter);

    public sealed class Handler(BillingDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(Query query, CancellationToken cancellationToken)
        {
            var normalizedPeriodType = query.PeriodType.Trim().ToLowerInvariant();
            ValidateQuery(normalizedPeriodType, query);

            var invoicesQuery = dbContext.Invoices
                .AsNoTracking()
                .Where(invoice => invoice.BillingYear == query.Year);

            invoicesQuery = normalizedPeriodType switch
            {
                "month" => invoicesQuery.Where(invoice => invoice.BillingMonth == query.Month!.Value),
                "quarter" => invoicesQuery.Where(invoice =>
                    invoice.BillingMonth >= ((query.Quarter!.Value - 1) * 3) + 1 &&
                    invoice.BillingMonth <= query.Quarter.Value * 3),
                "year" => invoicesQuery,
                _ => throw new ApiException("periodType must be month, quarter, or year.", StatusCodes.Status400BadRequest)
            };

            var summary = await invoicesQuery
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    TotalRevenue = group
                        .Where(invoice => invoice.Status == InvoiceStatus.Paid)
                        .Sum(invoice => invoice.TotalAmount),
                    TotalOutstanding = group
                        .Where(invoice => invoice.Status == InvoiceStatus.Unpaid)
                        .Sum(invoice => invoice.TotalAmount),
                    PaidInvoiceCount = group.Count(invoice => invoice.Status == InvoiceStatus.Paid),
                    UnpaidInvoiceCount = group.Count(invoice => invoice.Status == InvoiceStatus.Unpaid)
                })
                .FirstOrDefaultAsync(cancellationToken);

            return new Response(
                normalizedPeriodType,
                query.Year,
                normalizedPeriodType == "month" ? query.Month : null,
                normalizedPeriodType == "quarter" ? query.Quarter : null,
                summary?.TotalRevenue ?? 0,
                summary?.TotalOutstanding ?? 0,
                summary?.PaidInvoiceCount ?? 0,
                summary?.UnpaidInvoiceCount ?? 0);
        }

        private static void ValidateQuery(string normalizedPeriodType, Query query)
        {
            if (query.Year is < 2020 or > 2100)
            {
                throw new ApiException("year must be between 2020 and 2100.", StatusCodes.Status400BadRequest);
            }

            switch (normalizedPeriodType)
            {
                case "month":
                    if (query.Month is null or < 1 or > 12)
                    {
                        throw new ApiException("month must be between 1 and 12 when periodType is month.", StatusCodes.Status400BadRequest);
                    }
                    break;
                case "quarter":
                    if (query.Quarter is null or < 1 or > 4)
                    {
                        throw new ApiException("quarter must be between 1 and 4 when periodType is quarter.", StatusCodes.Status400BadRequest);
                    }
                    break;
                case "year":
                    break;
                default:
                    throw new ApiException("periodType must be month, quarter, or year.", StatusCodes.Status400BadRequest);
            }
        }
    }
}
