using Aspire.Hosting.Docker.Resources.ServiceNodes;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("compose");

// var postgres = builder.AddPostgres("postgres")
//     .WithPgAdmin();
// var identityDb = postgres.AddDatabase("identitydb");

var identityDb = builder.AddConnectionString("identitydb");
var profileDb = builder.AddConnectionString("profiledb");
var roomDb = builder.AddConnectionString("roomdb");
var bookingDb = builder.AddConnectionString("bookingdb");
var communityDb = builder.AddConnectionString("communitydb");

var incidentDb = builder.AddConnectionString("incidentdb");


var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume();

var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(identityDb);

var profileApi = builder.AddProject<Projects.Profile_API>("profile-api")
    .WithReference(profileDb);

var roomApi = builder.AddProject<Projects.RoomService_API>("room-api")
    .WithReference(roomDb);

var bookingApi = builder.AddProject<Projects.BookingService_API>("booking-api")
    .WithReference(bookingDb)
    .WithReference(roomApi)
    .WithReference(rabbitMq) 
    .WaitFor(roomApi);

var communityApi = builder.AddProject<Projects.Community_API>("community-api")
    .WithReference(communityDb);

var incidentApi = builder.AddProject<Projects.Incident_API>("incident-api")
    .WithReference(incidentDb)
    .WithReference(rabbitMq);

var gateway = builder.AddProject<Projects.Gateway_API>("gateway-api")
    .WithReference(identityApi)
    .WithReference(roomApi)
    .WithReference(profileApi)
    .WithReference(bookingApi)
    .WithReference(communityApi)
    .WithReference(incidentApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();
