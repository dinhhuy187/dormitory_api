var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("compose");

// var postgres = builder.AddPostgres("postgres")
//     .WithPgAdmin();
// var identityDb = postgres.AddDatabase("identitydb");

var identityDb = builder.AddConnectionString("identitydb");
var profileDb = builder.AddConnectionString("profiledb");

var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(identityDb);

var profileApi = builder.AddProject<Projects.Profile_API>("profile-api")
    .WithReference(profileDb);
builder.Build().Run();
