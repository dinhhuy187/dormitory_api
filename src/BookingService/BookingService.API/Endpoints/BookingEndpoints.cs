using BookingService.Application.Common.Models;
using BookingService.Application.UseCases.Bookings.Commands.CreateBooking;
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
        .Produces<CreateBookingResponse>(StatusCodes.Status201Created);
    }
}