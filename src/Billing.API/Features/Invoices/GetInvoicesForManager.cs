using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Invoices;

public static class GetInvoicesForManager
{
    public sealed record ManagerInvoiceListItemResponse(
        Guid InvoiceId,
        Guid RoomId,
        string? BuildingCode,
        int? Floor,
        Guid StudentId,
        short Month,
        int Year,
        decimal TotalAmount,
        string Status,
        DateTime? PaidAt,
        DateTime CreatedAt);

    public sealed record Query(
        string? BuildingCode,
        int? Floor,
        int? Year,
        short? Month,
        InvoiceStatus? Status,
        int Page,
        int PageSize);

    public sealed record PagedResult(IReadOnlyList<ManagerInvoiceListItemResponse> Items, int TotalItems, int Page, int PageSize);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/billing/invoices", async (
                    string? buildingCode,
                    int? floor,
                    int? year,
                    short? month,
                    InvoiceStatus? status,
                    int? page,
                    int? pageSize,
                    Handler handler,
                    CancellationToken ct) =>
                {
                    var result = await handler.ExecuteAsync(
                        new Query(buildingCode, floor, year, month, status, page ?? 1, pageSize ?? 20),
                        ct);

                    return Results.Ok(new ApiResponse<IReadOnlyList<ManagerInvoiceListItemResponse>>(
                        result.Items,
                        new PaginationMetadata(result.TotalItems, result.PageSize, result.Page)));
                })
                .WithTags("Billing - Invoices")
                .WithName("GetInvoicesForManager")
                .WithDescription("Required roles: Manager, Admin, or SeniorManager. Lists invoices for staff review. Optional filters are buildingCode, floor, year, month, and status. buildingCode and floor are queried from Billing invoice snapshots, and this endpoint does not call RoomService. Status enum values are Unpaid, Paid, and Canceled. Pagination defaults are page=1 and pageSize=20.")
                .RequireAuthorization(policy => policy.RequireRole("Manager", "Admin", "SeniorManager"))
                .Produces<IReadOnlyList<ManagerInvoiceListItemResponse>>(StatusCodes.Status200OK);
        }
    }

    public sealed class Handler(BillingDbContext dbContext)
    {
        public async Task<PagedResult> ExecuteAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var invoicesQuery = dbContext.Invoices.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.BuildingCode))
            {
                var normalizedBuildingCode = query.BuildingCode.Trim().ToUpper();
                invoicesQuery = invoicesQuery.Where(invoice =>
                    invoice.BuildingCode != null &&
                    invoice.BuildingCode.ToUpper() == normalizedBuildingCode);
            }

            if (query.Floor.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(invoice => invoice.Floor == query.Floor.Value);
            }

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
                .Select(invoice => new ManagerInvoiceListItemResponse(
                    invoice.Id,
                    invoice.RoomId,
                    invoice.BuildingCode,
                    invoice.Floor,
                    invoice.StudentId,
                    invoice.BillingMonth,
                    invoice.BillingYear,
                    invoice.TotalAmount,
                    invoice.Status.ToString(),
                    invoice.PaidAt,
                    invoice.CreatedAt))
                .ToListAsync(cancellationToken);

            return new PagedResult(invoices, totalItems, page, pageSize);
        }
    }
}
