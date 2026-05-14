using System.Security.Claims;
using BookingService.Application.Common.Models;
using BookingService.Application.UseCases.Bookings.Commands.CreateBooking;
using BookingService.Application.UseCases.Bookings.Queries.GetUserBookings;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace BookingService.API.Endpoints;

public static class BookingEndpoints
{
    public static void MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings")
                       .WithTags("Bookings")
                       .RequireAuthorization(); // Bắt buộc phải có token đăng nhập (JWT)

        // API XEM TẤT CẢ ĐƠN ĐẶT PHÒNG CỦA USER
        group.MapGet("/", async (
            Guid? userId,
            HttpContext httpContext,
            [FromServices] IGetUserBookingsUseCase useCase,
            CancellationToken ct) =>
        {
            var currentUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? httpContext.User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserGuid))
            {
                return Results.Unauthorized();
            }

            var isAdminOrManager = httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("Manager");
            var targetUserId = userId ?? currentUserGuid;

            if (!isAdminOrManager && targetUserId != currentUserGuid)
            {
                return Results.Forbid();
            }

            var result = await useCase.ExecuteAsync(targetUserId, ct);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { Error = result.ErrorMessage });
            }

            return Results.Ok(new ApiResponse<List<BookingItemResponse>>(result.Value!));
        })
        .WithName("GetUserBookings")
        .Produces<List<BookingItemResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);


        // API TẠO ĐƠN ĐẶT PHÒNG
        group.MapPost("/", async (
            [FromBody] CreateBookingRequest request,
            [FromServices] ICreateBookingUseCase useCase,
            CancellationToken ct) =>
        {
            // Gọi Use Case ở tầng Application
            var result = await useCase.ExecuteAsync(request, ct);

            if (result.IsSuccess)
            {
                // HTTP 201: Tạo thành công. Kèm theo đường dẫn để lấy thông tin đơn vừa tạo
                return Results.Created($"/api/bookings/{result.Value!.BookingId}", result.Value);
            }

            // HTTP 400: Lỗi nghiệp vụ (ví dụ: Hết phòng, sai quy định)
            return Results.BadRequest(new { Error = result.ErrorMessage });
        })
        .WithName("CreateBooking")
        .AddEndpointFilter<ValidationFilter<CreateBookingRequest>>()
        .WithDescription("Term Name có các giá trị là học kỳ 1, 2, hè và năm học: HK1_2025_2026, HK2_2025_2026, HKH_2025_2026, HK1_2026_2027, HK2_2026_2027")
        .Produces<CreateBookingResponse>(StatusCodes.Status201Created);
    }
}