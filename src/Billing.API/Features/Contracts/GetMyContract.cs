using Billing.API.Infrastructure.Auth;
using Billing.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Contracts;

public static class GetMyContract
{
    public sealed record Response(
        Guid StudentId,
        Guid? LatestInvoiceId,
        Guid? RoomId,
        Guid ContractTemplateId,
        string Code,
        string Name,
        int Version,
        string Content,
        DateOnly EffectiveFrom,
        DateOnly? EffectiveTo,
        string Source);

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

                    var response = await handler.ExecuteAsync(studentId, ct);
                    return Results.Ok(new ApiResponse<Response>(response));
                })
                .WithTags("Billing - Contracts")
                .WithName("GetMyContract")
                .WithDescription("Required role: Student. Gets contract template information for the authenticated student. Uses JWT user id as StudentId. Source is LatestInvoice when an invoice has a contract snapshot, otherwise ActiveTemplate.")
                .RequireAuthorization(policy => policy.RequireRole("Student"))
                .Produces<Response>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized);
        }
    }

    public sealed class Handler(BillingDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(Guid studentId, CancellationToken cancellationToken)
        {
            var latestInvoice = await dbContext.Invoices
                .AsNoTracking()
                .Include(invoice => invoice.ContractTemplate)
                .Where(invoice => invoice.StudentId == studentId)
                .OrderByDescending(invoice => invoice.BillingYear)
                .ThenByDescending(invoice => invoice.BillingMonth)
                .ThenByDescending(invoice => invoice.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var template = latestInvoice?.ContractTemplate;
            var source = "LatestInvoice";

            if (template is null)
            {
                source = "ActiveTemplate";
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                template = await dbContext.ContractTemplates
                    .AsNoTracking()
                    .Where(contractTemplate => contractTemplate.IsActive &&
                                               contractTemplate.EffectiveFrom <= today &&
                                               (contractTemplate.EffectiveTo == null || contractTemplate.EffectiveTo >= today))
                    .OrderByDescending(contractTemplate => contractTemplate.EffectiveFrom)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (template is null)
            {
                throw new ApiException("Contract template not found.", StatusCodes.Status404NotFound);
            }

            return new Response(
                studentId,
                latestInvoice?.Id,
                latestInvoice?.RoomId,
                template.Id,
                template.Code,
                template.Name,
                template.Version,
                template.Content,
                template.EffectiveFrom,
                template.EffectiveTo,
                source);
        }
    }
}
