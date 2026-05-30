using DotNetEnv;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Profile.API.Features.Profile;
using Profile.API.Infrastructure.Database;
using Shared;
using Shared.Endpoints;
using Shared.Extensions;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var grpcPort = builder.Configuration.GetValue<int?>("PROFILE_GRPC_PORT");
if (grpcPort is not null)
{
    builder.WebHost.ConfigureKestrel((context, options) =>
    {
        foreach (var httpPort in GetConfiguredHttpPorts(context.Configuration))
        {
            options.ListenAnyIP(httpPort, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1;
            });
        }

        options.ListenAnyIP(grpcPort.Value, listenOptions =>
        {
            options.ListenAnyIP(grpcPort.Value, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });
    });
}

builder.AddNpgsqlDbContext<ProfileDbContext>("profiledb");

builder.Services.AddCustomJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddGrpc();
builder.Services.AddScoped<IMediaService, CloudinaryMediaService>();
builder.Services.AddHandlersFromAssemblyContaining<Program>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddOpenApi(options =>
{
    options.CreateSchemaReferenceId = (type) => type.Type.FullName ?? type.Type.Name;
});

// Add services to the container.
builder.Services.AddEndpoints(typeof(Program).Assembly);
var app = builder.Build();

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
app.MapGrpcService<ProfileReaderGrpcService>();
app.MapEndpoints();
app.Run();

static IReadOnlyCollection<int> GetConfiguredHttpPorts(IConfiguration configuration)
{
    var ports = new HashSet<int>();

    AddPorts(configuration["ASPNETCORE_HTTP_PORTS"]);
    AddPorts(configuration["HTTP_PORTS"]);

    var urls = configuration["urls"] ?? configuration["ASPNETCORE_URLS"];
    if (!string.IsNullOrWhiteSpace(urls))
    {
        foreach (var url in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttp)
            {
                ports.Add(uri.Port);
            }
        }
    }

    if (ports.Count == 0)
    {
        ports.Add(8080);
    }

    return ports;

    void AddPorts(string? portList)
    {
        if (string.IsNullOrWhiteSpace(portList))
        {
            return;
        }

        foreach (var port in portList.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(port, out var parsedPort))
            {
                ports.Add(parsedPort);
            }
        }
    }
}