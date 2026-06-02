using Identity.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;

namespace Identity.API.Features.UsersManagement;

public static class SyncStudentUsers
{
    public sealed record Response(string Id, string UserName, string Email, string FullName);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/auth/users/students/sync", async (
                    ApplicationDbContext dbContext,
                    CancellationToken ct) =>
                {
                    var studentRoleId = await dbContext.Roles
                        .AsNoTracking()
                        .Where(role => role.Name == "Student")
                        .Select(role => role.Id)
                        .FirstOrDefaultAsync(ct);

                    if (string.IsNullOrWhiteSpace(studentRoleId))
                    {
                        return Results.Ok(Array.Empty<Response>());
                    }

                    var studentUserIds = dbContext.UserRoles
                        .Where(userRole => userRole.RoleId == studentRoleId)
                        .Select(userRole => userRole.UserId);

                    var students = await dbContext.Users
                        .AsNoTracking()
                        .Where(user => studentUserIds.Contains(user.Id) && user.IsActive)
                        .OrderBy(user => user.UserName)
                        .Select(user => new Response(
                            user.Id,
                            user.UserName ?? string.Empty,
                            user.Email ?? string.Empty,
                            user.FullName))
                        .ToListAsync(ct);

                    return Results.Ok(students);
                })
                .WithTags("Users Management - Sync")
                .WithName("SyncStudentUsers")
                .WithDescription("Internal service-to-service sync endpoint for seed jobs. Returns active student users as a raw JSON list and does not require a user JWT.")
                .Produces<IReadOnlyList<Response>>(StatusCodes.Status200OK);
        }
    }
}
