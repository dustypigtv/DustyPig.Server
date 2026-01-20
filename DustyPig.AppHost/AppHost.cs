using DustyPig.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var seq = builder
    .AddSeq("seq")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("seq")
    .WithEnvironment("ACCEPT_EULA", "Y");

var sharedFileStore = builder.AddFileStore("db-shared", "db-shared");

var postgresdb = builder
    .AddPostgres("postgres")
    .WithPgAdmin_MyVersion(fileStore: sharedFileStore)
    .WithDataVolume("postgres")
    .WithFileStore(sharedFileStore, "/db-shared")
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("dustypig-v3");

builder
    .AddProject<Projects.DustyPig_Server>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WithReference(seq)
    .WaitFor(seq)
    .WithFileStore(sharedFileStore);

builder.Build().Run();
