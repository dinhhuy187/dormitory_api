using System.Reflection;
using Billing.API.Infrastructure.Database;
using Billing.API.Infrastructure.Services;
using DotNetEnv;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using Shared.Extensions;
using Shared.Grpc.Rooms;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.AddNpgsqlDbContext<BillingDbContext>("billingdb");

var roomGrpcAddress = builder.Configuration["Services:room-api:grpc:0"] ?? "http://room-api:8082";
builder.Services.AddGrpcClient<RoomBillingReader.RoomBillingReaderClient>(options =>
{
    options.Address = new Uri(roomGrpcAddress);
});
builder.Services.AddScoped<IRoomBillingClient, RoomBillingClient>();

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

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    await dbContext.Database.MigrateAsync();
    await SeedData.SeedAsync(dbContext);
}

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapOpenApi("api/billing/openapi/v1.json");

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.Run();
