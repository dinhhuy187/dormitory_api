using BookingService.Application.Abtractions.Data;
using BookingService.Application.Abtractions.Services;
using BookingService.Domain.Repositories;
using BookingService.Domain.Services;
using BookingService.Infrastructure.Data;
using BookingService.Infrastructure.EventHandlers.Internal;
using BookingService.Infrastructure.Repositories;
using BookingService.Infrastructure.Sagas;
using BookingService.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureLayer(
        this IServiceCollection services, 
        IConfiguration configuration)
    {

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<BookingDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });
            // x.AddConsumer<RoomPriceupdatedEventHandler>();
            x.AddConsumer<CancelBookingCommandConsumer>();
            x.AddConsumer<ConfirmBookingCommandConsumer>();

            x.AddSagaStateMachine<BookingStateMachine, BookingSagaState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ExistingDbContext<BookingDbContext>();
                    r.UsePostgres();
                });

            x.UsingRabbitMq((context, cfg) =>
            {
                var connectionString = configuration.GetConnectionString("rabbitmq");
                cfg.Host(connectionString);
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAcademicTermRepository, AcademicTermRepository>();
        services.AddScoped<IRoomPricingService, RoomPricingService>();
        services.AddScoped<IBookingRulesChecker, BookingRulesChecker>();
        services.AddScoped<IRegistrationPeriodChecker, RegistrationPeriodChecker>();
        services.AddScoped<IFeeTemplateRepository, FeeTemplateRepository>();    
        // Nhớ đăng ký thêm các service khác ở đây
        // services.AddScoped<IAcademicTermRepository, AcademicTermRepository>();
        // services.AddScoped<IRoomPricingService, RoomPricingService>();

        return services;
    }
}