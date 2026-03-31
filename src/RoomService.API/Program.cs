using System.Text.Json.Serialization;
using Carter;
using DotNetEnv;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Infrastructure.Database;
using RoomService.API.Infrastructure.PipelineBehaviors;
using Shared;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddCustomJwtAuthentication(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddNpgsqlDbContext<RoomDbContext>("roomdb");

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddCarter();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("api/rooms/openapi/v1.json");
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<RoomDbContext>();
    dbContext.Database.Migrate();
    try
    {
        await SeedData.SeedAsync(dbContext);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra trong quá trình Migrate và Seed dữ liệu.");
    }
}

app.MapCarter();

app.Run();
