using System.Security.Claims;
using BookingService.Application.Common.Models;
using BookingService.Application.UseCases.Bookings.Commands.CheckoutBooking;
using BookingService.Application.UseCases.Bookings.Commands.CreateBooking;
using BookingService.Application.UseCases.Bookings.Queries.GetMyCurrentRoom;
using BookingService.Application.UseCases.Bookings.Queries.GetRoomStudents;
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

        group.MapGet("/me/current-room", async (
            HttpContext httpContext,
            [FromServices] IGetMyCurrentRoomUseCase useCase,
            CancellationToken ct) =>
        {
            var currentUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? httpContext.User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserGuid))
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(currentUserGuid, ct);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { Error = result.ErrorMessage });
            }

            return Results.Ok(new ApiResponse<CurrentRoomResponse?>(result.Value));
        })
        .WithName("GetMyCurrentRoom")
        .WithDescription("Required authentication. Gets the authenticated user's current room from their Active or Confirmed booking. Returns null when the user has no current room.")
        .RequireAuthorization()
        .Produces<CurrentRoomResponse?>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/rooms/students", async (
            HttpContext httpContext,
            [FromServices] IGetRoomStudentsUseCase useCase,
            CancellationToken ct) =>
        {
            var currentUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? httpContext.User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserGuid))
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(new GetRoomStudentsQuery(currentUserGuid), ct);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { Error = result.ErrorMessage });
            }

            return Results.Ok(new ApiResponse<RoomStudentsResponse>(result.Value!));
        })
        .WithName("GetRoomStudents")
        .WithDescription("Required authentication. Gets the authenticated user's current room from their Active or Confirmed booking, then returns students assigned to that room and room detail equivalent to GET /api/rooms/{id}. Room fields come from RoomService through gRPC; booking fields come from BookingService; profile fields come from ProfileService through gRPC. Includes only Confirmed and Active bookings; excludes Pending, Canceled, and Completed. Citizen ID, address, ethnicity, religion, and emergency contact fields are intentionally not returned.")
        .RequireAuthorization()
        .Produces<RoomStudentsResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status502BadGateway)
        .Produces(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/rooms/{roomId:guid}/students", async (
            Guid roomId,
            [FromServices] IGetRoomStudentsUseCase useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteByRoomIdAsync(new GetRoomStudentsByRoomQuery(roomId), ct);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { Error = result.ErrorMessage });
            }

            return Results.Ok(new ApiResponse<RoomStudentsResponse>(result.Value!));
        })
        .WithName("GetRoomStudentsForManager")
        .WithDescription("Required roles: Admin, Manager, or SeniorManager. Gets students assigned to the specified roomId and room detail equivalent to GET /api/rooms/{id}. Room fields come from RoomService through gRPC; booking fields come from BookingService; profile fields come from ProfileService through gRPC. Includes only Confirmed and Active bookings; excludes Pending, Canceled, and Completed. Citizen ID, address, ethnicity, religion, and emergency contact fields are intentionally not returned.")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "SeniorManager"))
        .Produces<RoomStudentsResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status502BadGateway)
        .Produces(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/users/{userId:guid}/checkout", async (
            Guid userId,
            [FromServices] ICheckoutBookingUseCase useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(userId, ct);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { Error = result.ErrorMessage });
            }

            return Results.Ok(new ApiResponse<CheckoutBookingResponse>(result.Value!));
        })
        .WithName("CheckoutStudentBooking")
        .WithDescription("Required roles: Admin, Manager, or SeniorManager. Checks out the specified student by userId from their current Active booking. The booking is changed to Completed, then BookingService publishes a room-capacity release command so RoomService and BookingService room data update occupied count.")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "SeniorManager"))
        .Produces<CheckoutBookingResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
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
        .WithDescription("Term Name chỉ nhận giá trị HK2_2025_2026.")
        .Produces<CreateBookingResponse>(StatusCodes.Status201Created);
    }
}
