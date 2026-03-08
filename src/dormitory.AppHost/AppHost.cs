var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("compose");

// var postgres = builder.AddPostgres("postgres")
//     .WithPgAdmin();
// var identityDb = postgres.AddDatabase("identitydb");

var identityDb = builder.AddConnectionString("identitydb");

var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(identityDb);
builder.Build().Run();
