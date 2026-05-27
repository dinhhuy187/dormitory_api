using System.Reflection;
using Billing.API.Infrastructure.Database;
using DotNetEnv;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using Shared.Extensions;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.AddNpgsqlDbContext<BillingDbContext>("billingdb");

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

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapOpenApi("api/billing/openapi/v1.json");

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.Run();
