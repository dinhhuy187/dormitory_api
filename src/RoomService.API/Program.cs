using System.Reflection;
using System.Text.Json.Serialization;
using DotNetEnv;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Features.Rooms;
using RoomService.API.Infrastructure.Database;
using Shared.Endpoints;
using Shared.Extensions;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.AddServiceDefaults();

var grpcPort = builder.Configuration.GetValue<int?>("ROOM_GRPC_PORT");
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
            listenOptions.Protocols = HttpProtocols.Http2;
        });
    });
}

// Add services to the container.
builder.Services.AddCustomJwtAuthentication(builder.Configuration);
builder.Services.AddGrpc();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.CreateSchemaReferenceId = (type) => type.Type.FullName ?? type.Type.Name;
});

builder.AddNpgsqlDbContext<RoomDbContext>("roomdb");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ReserveRoomCapacityCommandConsumer>();
    x.AddConsumer<ReleaseRoomCapacityCommandConsumer>();

    x.AddEntityFrameworkOutbox<RoomDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseEntityFrameworkOutbox<RoomDbContext>(context);
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbitmq"));
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddHandlersFromAssemblyContaining<Program>();
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();

app.MapOpenApi("api/rooms/openapi/v1.json");
// Configure the HTTP request pipeline.

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

app.MapGrpcService<RoomBillingGrpcService>();
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
