using FluentValidation;
using Incident.API.Domain.Enums;
using Incident.API.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Incident.API.Features;

public static class UpdateIncidentStatus
{
    public record Request(string Status);

    public record Response(
        Guid Id,
        string Status,
        DateTime UpdatedAt
    );

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status không được để trống.")
                .Must(s => Enum.TryParse<IncidentStatus>(s, ignoreCase: true, out _))
                .WithMessage($"Status không hợp lệ. Các giá trị hợp lệ: {string.Join(", ", Enum.GetNames<IncidentStatus>())}");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("api/incidents/{id}/status", async (
                Guid id,
                [FromBody] Request request,
                [FromServices] Handler handler,
                [FromServices] IValidator<Request> validator,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                    return Results.ValidationProblem(validation.ToDictionary());

                var result = await handler.ExecuteAsync(id, request, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Incidents")
            .WithName("UpdateIncidentStatus")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Staff"))
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
        }
    }

    public class Handler(IncidentDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(Guid id, Request req, CancellationToken ct)
        {
            var incident = await dbContext.Incidents
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (incident is null)
                throw new ApiException($"Không tìm thấy incident với id {id}.", StatusCodes.Status404NotFound);

            incident.Status = Enum.Parse<IncidentStatus>(req.Status, ignoreCase: true);
            incident.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(ct);

            return new Response(incident.Id, incident.Status.ToString(), incident.UpdatedAt);
        }
    }
}