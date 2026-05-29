using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Auth;
using Billing.API.Infrastructure.Database;
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

                    var result = await handler.ExecuteAsync(
                        new Query(year, month, status, page ?? 1, pageSize ?? 20),
                        studentId,
                        ct);

                    return Results.Ok(new ApiResponse<IReadOnlyList<InvoiceListItemResponse>>(
                        result.Items,
                        new PaginationMetadata(result.TotalItems, result.PageSize, result.Page)));
                })
                .WithTags("Billing - Invoices")
                .WithName("GetMyInvoices")
                .WithDescription("Required role: Student. Lists invoices for the authenticated student. Optional filters are year, month, and status. Status enum values are Unpaid and Paid. Pagination uses page and pageSize; defaults are page=1 and pageSize=20.")
                .RequireAuthorization(policy => policy.RequireRole("Student"))
                .Produces<IReadOnlyList<InvoiceListItemResponse>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized);
        }
    }

    public sealed record Query(int? Year, short? Month, InvoiceStatus? Status, int Page, int PageSize);

    public sealed record PagedResult(IReadOnlyList<InvoiceListItemResponse> Items, int TotalItems, int Page, int PageSize);

    public sealed class Handler(BillingDbContext dbContext)
    {
        public async Task<PagedResult> ExecuteAsync(Query query, Guid studentId, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var invoicesQuery = dbContext.Invoices
                .AsNoTracking()
                .Where(invoice => invoice.StudentId == studentId);

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

            return new PagedResult(
                invoices.Select(InvoiceResponseMapper.ToListItem).ToList(),
                totalItems,
                page,
                pageSize);
        }
    }
}
