using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Profile.API.Data;
using Profile.API.Services;
using System.Text;
using Shared;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.AddNpgsqlDbContext<ProfileDbContext>("profiledb");

builder.Services.AddCustomJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IMediaService, CloudinaryMediaService>();
builder.Services.AddOpenApi();

// Add services to the container.

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
app.MapControllers();
app.Run();