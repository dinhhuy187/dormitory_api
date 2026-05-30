using Chat.API.Infrastructure.Database;
using Chat.API.Hubs;
using Shared.Services;
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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("null", "http://localhost:5500", "http://127.0.0.1:5500")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // bắt buộc cho SignalR
    });
});

builder.AddNpgsqlDbContext<ChatDbContext>("chatdb");

builder.Services.AddCustomJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddScoped<IMediaService, CloudinaryMediaService>();

// Aspire Service Discovery tự resolve "profile-api" → đúng URL
builder.Services.AddHttpClient<IProfileService, ProfileService>(client =>
{
    client.BaseAddress = new Uri("https+http://profile-api");
})
.AddServiceDiscovery();

builder.Services.AddHandlersFromAssemblyContaining<Program>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddOpenApi();

// SignalR — chuẩn bị sẵn, sẽ dùng ở bước sau
builder.Services.AddSignalR();

var app = builder.Build();

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapOpenApi("api/conversations/openapi/v1.json");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    db.Database.Migrate();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();
app.MapHub<ChatHub>("/api/conversations/hubs/chat");
app.Run();