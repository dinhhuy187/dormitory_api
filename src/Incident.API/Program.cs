using DotNetEnv;
using FluentValidation;
using Incident.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using Shared.Extensions;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();

builder.AddNpgsqlDbContext<IncidentDbContext>("incidentdb");

builder.Services.AddCustomJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddHandlersFromAssemblyContaining<Program>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.MapDefaultEndpoints();

app.MapOpenApi("api/incidents/openapi/v1.json");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IncidentDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();
app.Run();