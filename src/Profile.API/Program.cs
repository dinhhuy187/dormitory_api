using DotNetEnv;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Profile.API.Infrastructure.Database;
using Shared;
using Shared.Endpoints;
using Shared.Extensions;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();

builder.AddNpgsqlDbContext<ProfileDbContext>("profiledb");

builder.Services.AddCustomJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddScoped<IMediaService, CloudinaryMediaService>();
builder.Services.AddHandlersFromAssemblyContaining<Program>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddOpenApi();

// Add services to the container.
builder.Services.AddEndpoints(typeof(Program).Assembly);
var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseExceptionHandler();
app.MapDefaultEndpoints();

app.MapOpenApi("api/profile/openapi/v1.json");
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProfileDbContext>();
    db.Database.Migrate(); // ← tự migrate khi start
}

app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();
app.Run();