using DotNetEnv;
using FluentValidation;
using Community.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Extensions;
using Shared;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();

builder.AddNpgsqlDbContext<CommunityDbContext>("communitydb");
builder.Services.AddScoped<IMediaService, CloudinaryMediaService>();
builder.Services.AddCustomJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddHandlersFromAssemblyContaining<Program>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapOpenApi("api/community/openapi/v1.json");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();
app.Run();