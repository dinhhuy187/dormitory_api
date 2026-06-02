using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Reports;

public static class GetFinancialSummary
{
    private const string TypeMonth = "MONTH";
    private const string TypeQuarter = "QUARTER";
    private const string TypeYear = "YEAR";

    public sealed record Response(
        decimal TotalRevenue,
        decimal TotalDebt,
        int PaidInvoices,
        int PendingInvoices,
        int WaitForConfirmInvoices,
        decimal WaitForConfirmAmount,
        RevenueChartResponse RevenueChart,
        IReadOnlyList<DebtChartItemResponse> DebtChart);

    public sealed record RevenueChartResponse(
        IReadOnlyList<string> Labels,
        IReadOnlyList<RevenueDatasetResponse> Datasets);

    public sealed record RevenueDatasetResponse(IReadOnlyList<decimal> Data);

    public sealed record DebtChartItemResponse(
        string Name,
        decimal Population,
        string Color,
        string LegendFontColor,
        int LegendFontSize);

    public sealed record Query(string Type, int? Month, int Year);

    private sealed record PeriodInfo(
        string Type,
        int? Month,
        int Year,
        int StartMonth,
        int EndMonth,
        DateOnly PeriodStart);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/billing/reports/financial-summary", async (
                    string type,
                    int? month,
                    int year,
                    Handler handler,
                    CancellationToken ct) =>
                {
                    var response = await handler.ExecuteAsync(new Query(type, month, year), ct);
                    return Results.Ok(new ApiResponse<Response>(response));
                })
                .WithTags("Billing - Reports")
                .WithName("GetFinancialSummary")
                .WithDescription("Required role: Admin. Returns finance KPIs and chart data. type accepts MONTH, QUARTER, or YEAR. month is required for MONTH as 1-12 and for QUARTER as 1-4; month is ignored for YEAR. Status values are Unpaid, WaitForConfirm, Paid, and Canceled; Canceled invoices are ignored. TotalDebt includes Unpaid and WaitForConfirm invoices. debtChart is empty for future QUARTER or YEAR periods.")
                .RequireAuthorization(policy => policy.RequireRole("Admin"))
                .Produces<Response>(StatusCodes.Status200OK)
                .ProducesValidationProblem();
        }
    }

    public sealed class Handler(BillingDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(Query query, CancellationToken cancellationToken)
        {
            var period = ValidateAndBuildPeriod(query);

            var invoicesQuery = dbContext.Invoices
                .AsNoTracking()
                .Where(invoice => invoice.Status != InvoiceStatus.Canceled &&
                                  invoice.BillingYear == period.Year &&
                                  invoice.BillingMonth >= period.StartMonth &&
                                  invoice.BillingMonth <= period.EndMonth);

            var summary = await invoicesQuery
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    TotalRevenue = group
                        .Where(invoice => invoice.Status == InvoiceStatus.Paid)
                        .Sum(invoice => invoice.TotalAmount),
                    UnpaidAmount = group
                        .Where(invoice => invoice.Status == InvoiceStatus.Unpaid)
                        .Sum(invoice => invoice.TotalAmount),
                    WaitForConfirmAmount = group
                        .Where(invoice => invoice.Status == InvoiceStatus.WaitForConfirm)
                        .Sum(invoice => invoice.TotalAmount),
                    PaidInvoices = group.Count(invoice => invoice.Status == InvoiceStatus.Paid),
                    PendingInvoices = group.Count(invoice => invoice.Status == InvoiceStatus.Unpaid),
                    WaitForConfirmInvoices = group.Count(invoice => invoice.Status == InvoiceStatus.WaitForConfirm)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var totalRevenue = summary?.TotalRevenue ?? 0;
            var waitForConfirmAmount = summary?.WaitForConfirmAmount ?? 0;
            var totalDebt = (summary?.UnpaidAmount ?? 0) + waitForConfirmAmount;
            var paidInvoices = summary?.PaidInvoices ?? 0;
            var pendingInvoices = summary?.PendingInvoices ?? 0;
            var waitForConfirmInvoices = summary?.WaitForConfirmInvoices ?? 0;

            var revenueChart = await BuildRevenueChartAsync(period, cancellationToken);
            var debtChart = BuildDebtChart(period, totalRevenue, totalDebt);

            return new Response(
                totalRevenue,
                totalDebt,
                paidInvoices,
                pendingInvoices,
                waitForConfirmInvoices,
                waitForConfirmAmount,
                revenueChart,
                debtChart);
        }

        private async Task<RevenueChartResponse> BuildRevenueChartAsync(
            PeriodInfo period,
            CancellationToken cancellationToken)
        {
            return period.Type switch
            {
                TypeMonth => await BuildMonthlyRevenueChartAsync(period, cancellationToken),
                TypeQuarter => await BuildMonthBucketRevenueChartAsync(
                    period,
                    Enumerable.Range(period.StartMonth, 3).Select(month => $"T{month}").ToList(),
                    cancellationToken),
                TypeYear => await BuildMonthBucketRevenueChartAsync(
                    period,
                    Enumerable.Range(1, 12).Select(month => $"T{month}").ToList(),
                    cancellationToken),
                _ => throw new ApiException("type must be MONTH, QUARTER, or YEAR.", StatusCodes.Status400BadRequest)
            };
        }

        private async Task<RevenueChartResponse> BuildMonthlyRevenueChartAsync(
            PeriodInfo period,
            CancellationToken cancellationToken)
        {
            var daysInMonth = DateTime.DaysInMonth(period.Year, period.StartMonth);
            var labels = Enumerable.Range(1, daysInMonth > 28 ? 5 : 4)
                .Select(week => $"W{week}")
                .ToList();
            var data = labels.Select(_ => 0m).ToArray();

            var paidInvoices = await dbContext.Invoices
                .AsNoTracking()
                .Where(invoice => invoice.Status == InvoiceStatus.Paid &&
                                  invoice.BillingYear == period.Year &&
                                  invoice.BillingMonth == period.StartMonth)
                .Select(invoice => new
                {
                    invoice.CreatedAt,
                    invoice.TotalAmount
                })
                .ToListAsync(cancellationToken);

            foreach (var invoice in paidInvoices)
            {
                var weekIndex = Math.Min((invoice.CreatedAt.Day - 1) / 7, labels.Count - 1);
                data[weekIndex] += invoice.TotalAmount;
            }

            return new RevenueChartResponse(labels, [new RevenueDatasetResponse(data)]);
        }

        private async Task<RevenueChartResponse> BuildMonthBucketRevenueChartAsync(
            PeriodInfo period,
            IReadOnlyList<string> labels,
            CancellationToken cancellationToken)
        {
            var revenueByMonth = await dbContext.Invoices
                .AsNoTracking()
                .Where(invoice => invoice.Status == InvoiceStatus.Paid &&
                                  invoice.BillingYear == period.Year &&
                                  invoice.BillingMonth >= period.StartMonth &&
                                  invoice.BillingMonth <= period.EndMonth)
                .GroupBy(invoice => invoice.BillingMonth)
                .Select(group => new
                {
                    Month = group.Key,
                    TotalRevenue = group.Sum(invoice => invoice.TotalAmount)
                })
                .ToListAsync(cancellationToken);

            var revenueLookup = revenueByMonth.ToDictionary(item => item.Month, item => item.TotalRevenue);
            var data = Enumerable.Range(period.StartMonth, period.EndMonth - period.StartMonth + 1)
                .Select(month => revenueLookup.GetValueOrDefault((short)month))
                .ToList();

            return new RevenueChartResponse(labels, [new RevenueDatasetResponse(data)]);
        }

        private static IReadOnlyList<DebtChartItemResponse> BuildDebtChart(
            PeriodInfo period,
            decimal totalRevenue,
            decimal totalDebt)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (period.Type is TypeQuarter or TypeYear && period.PeriodStart > today)
            {
                return [];
            }

            var total = totalRevenue + totalDebt;
            var paidPercent = total == 0 ? 0 : Math.Round(totalRevenue / total * 100, 2);
            var debtPercent = total == 0 ? 0 : Math.Round(totalDebt / total * 100, 2);

            return
            [
                new DebtChartItemResponse("Da thu", paidPercent, "#22C55E", "#334155", 12),
                new DebtChartItemResponse("Con no", debtPercent, "#EF4444", "#334155", 12)
            ];
        }

        private static PeriodInfo ValidateAndBuildPeriod(Query query)
        {
            if (string.IsNullOrWhiteSpace(query.Type))
            {
                throw new ApiException("type must be MONTH, QUARTER, or YEAR.", StatusCodes.Status400BadRequest);
            }

            if (query.Year is < 2020 or > 2100)
            {
                throw new ApiException("year must be between 2020 and 2100.", StatusCodes.Status400BadRequest);
            }

            var normalizedType = query.Type.Trim().ToUpperInvariant();
            return normalizedType switch
            {
                TypeMonth => BuildMonthPeriod(query),
                TypeQuarter => BuildQuarterPeriod(query),
                TypeYear => new PeriodInfo(TypeYear, null, query.Year, 1, 12, new DateOnly(query.Year, 1, 1)),
                _ => throw new ApiException("type must be MONTH, QUARTER, or YEAR.", StatusCodes.Status400BadRequest)
            };
        }

        private static PeriodInfo BuildMonthPeriod(Query query)
        {
            if (query.Month is null or < 1 or > 12)
            {
                throw new ApiException("month must be between 1 and 12 when type is MONTH.", StatusCodes.Status400BadRequest);
            }

            return new PeriodInfo(
                TypeMonth,
                query.Month,
                query.Year,
                query.Month.Value,
                query.Month.Value,
                new DateOnly(query.Year, query.Month.Value, 1));
        }

        private static PeriodInfo BuildQuarterPeriod(Query query)
        {
            if (query.Month is null or < 1 or > 4)
            {
                throw new ApiException("month must be between 1 and 4 when type is QUARTER.", StatusCodes.Status400BadRequest);
            }

            var startMonth = ((query.Month.Value - 1) * 3) + 1;
            return new PeriodInfo(
                TypeQuarter,
                query.Month,
                query.Year,
                startMonth,
                startMonth + 2,
                new DateOnly(query.Year, startMonth, 1));
        }
    }
}
