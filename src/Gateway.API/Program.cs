using DotNetEnv;
using Shared;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddCustomJwtAuthentication(builder.Configuration);

builder.Services.AddOpenApi();

//builder.Services.AddServiceDiscovery();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("api/auth/openapi/v1.json", "Identity Service API");
    options.SwaggerEndpoint("api/rooms/openapi/v1.json", "Room Service API");

    options.RoutePrefix = string.Empty;
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    
}


app.MapReverseProxy();

app.Run();
