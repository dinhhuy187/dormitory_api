using BookingService.Application.Common.Models;
using BookingService.Application.UseCases.FeeTemplates.Queries.GetAllFeeTemplates;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace BookingService.API.Endpoints;

public static class FeeTemplateEndpoints
{
    public static void MapFeeTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings/fee-templates")
                       .WithTags("Fee Templates")
                       .RequireAuthorization();

        group.MapGet("/", async (
            [FromServices] IGetAllFeeTemplatesUseCase useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(ct);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { Error = result.ErrorMessage });
            }

            return Results.Ok(new ApiResponse<List<FeeTemplateResponse>>(result.Value!));
        })
        .WithName("GetAllFeeTemplates")
        .Produces<List<FeeTemplateResponse>>(StatusCodes.Status200OK);
    }
}