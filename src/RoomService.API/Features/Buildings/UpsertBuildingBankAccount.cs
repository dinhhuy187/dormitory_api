using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Infrastructure.Database;
using Shared;
using Shared.Endpoints;

namespace RoomService.API.Features.Buildings
{
    public static class UpsertBuildingBankAccount
    {
        public record Command(
            string? BankCode,
            string? AccountNumber,
            string? AccountName
        );

        public record Response(
            Guid BuildingId,
            string? BankCode,
            string? AccountNumber,
            string? AccountName
        );

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.BankCode)
                    .MaximumLength(50).WithMessage("Mã ngân hàng không được vượt quá 50 ký tự.");

                RuleFor(x => x.AccountNumber)
                    .MaximumLength(50).WithMessage("Số tài khoản không được vượt quá 50 ký tự.");

                RuleFor(x => x.AccountName)
                    .MaximumLength(100).WithMessage("Tên chủ tài khoản không được vượt quá 100 ký tự.");
            }
        }

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/rooms/buildings/{id:guid}/bank-account", async (
                    Guid id,
                    [FromBody] Command command,
                    Handler handler,
                    CancellationToken ct) =>
                {
                    var result = await handler.ExecuteAsync(id, command, ct);
                    return Results.Ok(new ApiResponse<Response>(result));
                })
                .WithTags("Buildings")
                .WithName("UpsertBuildingBankAccount")
                .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "SeniorManager"))
                .AddEndpointFilter<ValidationFilter<Command>>()
                .Produces<Response>(StatusCodes.Status200OK);
            }
        }

        public class Handler(RoomDbContext dbContext)
        {
            public async Task<Response> ExecuteAsync(Guid buildingId, Command request, CancellationToken cancellationToken)
            {
                if (buildingId == Guid.Empty)
                {
                    throw new ApiException("ID tòa nhà không hợp lệ.", StatusCodes.Status400BadRequest);
                }

                var building = await dbContext.Buildings
                    .FirstOrDefaultAsync(b => b.Id == buildingId, cancellationToken);

                if (building is null)
                {
                    throw new ApiException("Không tìm thấy tòa nhà.", StatusCodes.Status404NotFound);
                }

                building.BankCode = Normalize(request.BankCode);
                building.AccountNumber = Normalize(request.AccountNumber);
                building.AccountName = Normalize(request.AccountName);

                await dbContext.SaveChangesAsync(cancellationToken);

                return new Response(
                    building.Id,
                    building.BankCode,
                    building.AccountNumber,
                    building.AccountName
                );
            }

            private static string? Normalize(string? value)
            {
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
        }
    }
}
