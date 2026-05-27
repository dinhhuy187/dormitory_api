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
var billingDb = builder.AddConnectionString("billingdb");
var chatDb = builder.AddConnectionString("chatdb");

var incidentDb = builder.AddConnectionString("incidentdb");


var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume();

var profileApi = builder.AddProject<Projects.Profile_API>("profile-api")
    .WithReference(profileDb)
    .WithEnvironment("PROFILE_GRPC_PORT", "8081")
    .WithEndpoint(name: "grpc", targetPort: 8081, scheme: "http")
    .WithEndpointsInEnvironment(endpoint => endpoint.Name != "grpc");

var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(identityDb)
    .WithReference(profileApi)
    .WaitFor(profileApi);

var roomApi = builder.AddProject<Projects.RoomService_API>("room-api")
    .WithReference(roomDb);

var bookingApi = builder.AddProject<Projects.BookingService_API>("booking-api")
    .WithReference(bookingDb)
    .WithReference(roomApi)
    .WithReference(rabbitMq)
    .WaitFor(roomApi);

var communityApi = builder.AddProject<Projects.Community_API>("community-api")
    .WithReference(communityDb)
    .WithReference(profileApi);

var incidentApi = builder.AddProject<Projects.Incident_API>("incident-api")
    .WithReference(incidentDb)
    .WithReference(rabbitMq);

var billingApi = builder.AddProject<Projects.Billing_API>("billing-api")
    .WithReference(billingDb);

var chatApi = builder.AddProject<Projects.Chat_API>("chat-api")
    .WithReference(chatDb)
    .WithReference(profileApi);

var gateway = builder.AddProject<Projects.Gateway_API>("gateway-api")
    .WithReference(identityApi)
    .WithReference(roomApi)
    .WithReference(profileApi)
    .WithReference(bookingApi)
    .WithReference(communityApi)
    .WithReference(incidentApi)
    .WithReference(chatApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();
