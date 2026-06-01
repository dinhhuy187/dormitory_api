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

// Add services to the container.

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

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.CreateSchemaReferenceId = (type) => type.Type.FullName ?? type.Type.Name;
});

builder.Services.AddHttpClient("RoomServiceClient", client =>
{
    client.BaseAddress = new Uri("http://room-api"); 
})
.AddStandardResilienceHandler();

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.MapDefaultEndpoints();

app.MapOpenApi("api/bookings/openapi/v1.json");

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
dbContext.Database.Migrate();
try
{
    await SeedData.SeedAsync(scope.ServiceProvider);
}
catch (Exception ex)
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Có lỗi xảy ra trong quá trình Migrate và Seed dữ liệu.");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapBookingSyncEndpoints();
app.MapFeeTemplateEndpoints();
app.MapBookingEndpoints();
app.Run();
