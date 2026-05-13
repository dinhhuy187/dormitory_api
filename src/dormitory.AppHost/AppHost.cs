using Aspire.Hosting.Docker.Resources.ServiceNodes;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("compose");

// var postgres = builder.AddPostgres("postgres")
//     .WithPgAdmin();
// var identityDb = postgres.AddDatabase("identitydb");

var identityDb = builder.AddConnectionString("identitydb");
var profileDb = builder.AddConnectionString("profiledb");
var roomDb = builder.AddConnectionString("roomdb");
var incidentDb = builder.AddConnectionString("incidentdb");

// RabbitMQ
var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(identityDb);

var profileApi = builder.AddProject<Projects.Profile_API>("profile-api")
    .WithReference(profileDb);

var roomApi = builder.AddProject<Projects.RoomService_API>("room-api")
    .WithReference(roomDb);

var incidentApi = builder.AddProject<Projects.Incident_API>("incident-api")
    .WithReference(incidentDb)
    .WithReference(rabbitMq);

var gateway = builder.AddProject<Projects.Gateway_API>("gateway-api")
    .WithReference(identityApi)
    .WithReference(roomApi)
    .WithReference(incidentApi)
    .WithExternalHttpEndpoints();


builder.Build().Run();
