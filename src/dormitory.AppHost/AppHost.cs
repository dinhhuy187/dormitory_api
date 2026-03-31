var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("compose");

// var postgres = builder.AddPostgres("postgres")
//     .WithPgAdmin();
// var identityDb = postgres.AddDatabase("identitydb");

var identityDb = builder.AddConnectionString("identitydb");
var roomDb = builder.AddConnectionString("roomdb");

var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(identityDb);

var roomApi = builder.AddProject<Projects.RoomService_API>("room-api")
    .WithReference(roomDb);

var gateway = builder.AddProject<Projects.Gateway_API>("gateway-api")
    .WithReference(identityApi)
    .WithReference(roomApi)
    .WithExternalHttpEndpoints();


builder.Build().Run();
