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

builder.Services.AddHttpClient("RoomServiceClient", client =>
{
    client.BaseAddress = new Uri("http://room-api");
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
var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("BillingSeedData");
await dbContext.Database.MigrateAsync();
await SeedData.SeedAsync(dbContext, httpClientFactory, seedLogger);

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapOpenApi("api/billing/openapi/v1.json");

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.Run();
