using Identity.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Identity.API.Features.UsersManagement;
public static class GetRolesSummary
{
    public record Response(string RoleName, int UserCount);
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/auth/users/summary", async (Handler handler, CancellationToken ct) =>
            {
                var result = await handler.ExecuteAsync(ct);
                return Results.Ok(result);
            })
            .WithTags("Users Management")
            .WithName("GetRolesSummary")
            .RequireAuthorization()
            .Produces<List<Response>>(StatusCodes.Status200OK);
        }
    }
    public class Handler(ApplicationDbContext dbContext)
    {
        public async Task<ApiResponse<List<Response>>> ExecuteAsync(CancellationToken cancellationToken)
        {
            var roleSummary = await dbContext.Roles
                .Select(r => new Response(
                    r.Name!,
                    dbContext.UserRoles.Count(ur => ur.RoleId == r.Id)
                ))
                .ToListAsync(cancellationToken);

            return new ApiResponse<List<Response>>(roleSummary);
        }
    }
}