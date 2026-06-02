using System.Reflection;
using Billing.API.Infrastructure.EventHandlers;
using Billing.API.Infrastructure.Database;
using Billing.API.Infrastructure.Services;
using DotNetEnv;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using Shared.Extensions;
using Shared.Grpc.Rooms;
using Shared.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.AddNpgsqlDbContext<BillingDbContext>("billingdb");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CreateBookingInvoiceCommandConsumer>();
    x.AddConsumer<CancelBookingInvoiceCommandConsumer>();

    x.AddEntityFrameworkOutbox<BillingDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseEntityFrameworkOutbox<BillingDbContext>(context);
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbitmq"));
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddGrpcClient<RoomBillingReader.RoomBillingReaderClient>(options =>
{
    options.Address = new Uri("http://_grpc.room-api");
});
builder.Services.AddScoped<IRoomBillingClient, RoomBillingClient>();

builder.Services.AddHttpClient("RoomServiceClient", client =>
{
    client.BaseAddress = new Uri("http://room-api");
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("BookingServiceClient", client =>
{
    client.BaseAddress = new Uri("http://booking-api");
})
.AddStandardResilienceHandler();

builder.Services.AddScoped<IBookingContractClient, BookingContractClient>();

builder.Services.AddHttpClient<IProfileService, ProfileService>(client =>
{
    client.BaseAddress = new Uri("http://profile-api");
})
.AddStandardResilienceHandler();

builder.Services.AddCustomJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddHandlersFromAssemblyContaining<Program>();
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

builder.Services.AddOpenApi(options =>
{
    options.CreateSchemaReferenceId = type => type.Type.FullName ?? type.Type.Name;
});

var app = builder.Build();

await using var scope = app.Services.CreateAsyncScope();
var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
var roomBillingClient = scope.ServiceProvider.GetRequiredService<IRoomBillingClient>();
var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("BillingSeedData");
await dbContext.Database.MigrateAsync();
await SeedData.SeedAsync(dbContext, httpClientFactory, roomBillingClient, seedLogger);

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapOpenApi("api/billing/openapi/v1.json");

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.Run();
