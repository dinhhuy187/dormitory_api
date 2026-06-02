using Billing.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Billing.API.Features.Contracts;

public static class GetContractTemplateByRoomType
{
    public sealed record Response(
        Guid ContractTemplateId,
        Guid? RoomTypeId,
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
            app.MapGet("/api/billing/contract-templates/room-types/{roomTypeId:guid}", async (
                    Guid roomTypeId,
                    DateOnly? effectiveDate,
                    Handler handler,
                    CancellationToken ct) =>
                {
                    var response = await handler.ExecuteAsync(roomTypeId, effectiveDate, ct);
                    return Results.Ok(new ApiResponse<Response>(response));
                })
                .WithTags("Billing - Contracts")
                .WithName("GetContractTemplateByRoomType")
                .WithDescription("Required roles: Student, Manager, Admin, or SeniorManager. Gets the active contract template for a RoomTypeId. effectiveDate is optional and defaults to the current UTC date. Exact room-type templates are preferred; otherwise the active generic fallback template is returned. Source values are RoomTypeTemplate and FallbackTemplate.")
                .RequireAuthorization(policy => policy.RequireRole("Student", "Manager", "Admin", "SeniorManager"))
                .Produces<Response>(StatusCodes.Status200OK);
        }
    }

    public sealed class Handler(BillingDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(
            Guid roomTypeId,
            DateOnly? effectiveDate,
            CancellationToken cancellationToken)
        {
            var selectedEffectiveDate = effectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var template = await dbContext.ContractTemplates
                .AsNoTracking()
                .Where(contractTemplate => contractTemplate.IsActive &&
                                           contractTemplate.RoomTypeId == roomTypeId &&
                                           contractTemplate.EffectiveFrom <= selectedEffectiveDate &&
                                           (contractTemplate.EffectiveTo == null || contractTemplate.EffectiveTo >= selectedEffectiveDate))
                .OrderByDescending(contractTemplate => contractTemplate.EffectiveFrom)
                .ThenByDescending(contractTemplate => contractTemplate.Version)
                .FirstOrDefaultAsync(cancellationToken);

            var source = "RoomTypeTemplate";
            if (template is null)
            {
                source = "FallbackTemplate";
                template = await dbContext.ContractTemplates
                    .AsNoTracking()
                    .Where(contractTemplate => contractTemplate.IsActive &&
                                               contractTemplate.RoomTypeId == null &&
                                               contractTemplate.EffectiveFrom <= selectedEffectiveDate &&
                                               (contractTemplate.EffectiveTo == null || contractTemplate.EffectiveTo >= selectedEffectiveDate))
                    .OrderByDescending(contractTemplate => contractTemplate.EffectiveFrom)
                    .ThenByDescending(contractTemplate => contractTemplate.Version)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (template is null)
            {
                throw new ApiException("Contract template not found.", StatusCodes.Status404NotFound);
            }

            return new Response(
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
    }
}
