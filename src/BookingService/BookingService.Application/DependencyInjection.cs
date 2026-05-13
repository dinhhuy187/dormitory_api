using BookingService.Application.Common.Models;
using BookingService.Application.UseCases.Bookings.Commands.CancelBooking;
using BookingService.Application.UseCases.Bookings.Commands.ConfirmBooking;
using BookingService.Application.UseCases.Bookings.Commands.CreateBooking;
using BookingService.Application.UseCases.Bookings.Queries.GetUserBookings;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<ICreateBookingUseCase, CreateBookingUseCase>();
        services.AddScoped<IConfirmBookingUseCase, ConfirmBookingUseCase>();
        services.AddScoped<ICancelBookingUseCase, CancelBookingUseCase>();
        services.AddScoped<IGetUserBookingsUseCase, GetUserBookingsUseCase>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}