using BookingService.Application.Common.Models;
using BookingService.Application.UseCases.Bookings.Commands.CreateBooking;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<ICreateBookingUseCase, CreateBookingUseCase>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}