using BookingService.API.Endpoints;
using BookingService.Application;
using BookingService.Infrastructure;
using BookingService.Infrastructure.Data;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Grpc.Profile;
using Shared.Grpc.Rooms;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCustomJwtAuthentication(builder.Configuration);

builder.AddNpgsqlDbContext<BookingDbContext>("bookingdb");

builder.AddServiceDefaults();

builder.Services.AddApplicationLayer();
builder.Services.AddInfrastructureLayer(builder.Configuration);

var profileGrpcAddress = builder.Configuration["Services:profile-api:grpc:0"] ?? "http://profile-api:8081";
builder.Services.AddGrpcClient<ProfileReader.ProfileReaderClient>(options =>
{
    options.Address = new Uri(profileGrpcAddress);
});

var roomGrpcAddress = builder.Configuration["Services:room-api:grpc:0"] ?? "http://room-api:8082";
builder.Services.AddGrpcClient<RoomBillingReader.RoomBillingReaderClient>(options =>
{
    options.Address = new Uri(roomGrpcAddress);
});

builder.Services.AddOpenApi(options =>
{
    options.CreateSchemaReferenceId = type => type.Type.FullName ?? type.Type.Name;
});

builder.Services.AddHttpClient("RoomServiceClient", client =>
{
    client.BaseAddress = new Uri("http://room-api");
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("IdentityServiceClient", client =>
{
    client.BaseAddress = new Uri("http://identity-api");
})
.AddStandardResilienceHandler();

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapOpenApi("api/bookings/openapi/v1.json");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    dbContext.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapBookingSyncEndpoints();
app.MapFeeTemplateEndpoints();
app.MapBookingEndpoints();

await app.StartAsync();

try
{
    await SeedData.SeedAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding BookingService data.");
}

await app.WaitForShutdownAsync();
