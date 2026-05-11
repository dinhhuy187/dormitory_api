using BookingService.API.Endpoints;
using BookingService.Application;
using BookingService.Infrastructure;
using BookingService.Infrastructure.Data;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;

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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.CreateSchemaReferenceId = (type) => type.Type.FullName ?? type.Type.Name;
});

var app = builder.Build();

app.UseExceptionHandler();
app.MapDefaultEndpoints();

app.MapOpenApi("api/bookings/openapi/v1.json");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    dbContext.Database.Migrate();
    // try
    // {
    //     await SeedData.SeedAsync(dbContext);
    // }
    // catch (Exception ex)
    // {
    //     var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    //     logger.LogError(ex, "Có lỗi xảy ra trong quá trình Migrate và Seed dữ liệu.");
    // }
}

app.MapBookingEndpoints();
app.Run();
