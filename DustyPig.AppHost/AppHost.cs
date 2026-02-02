using DustyPig.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("docker-compose");

var seq = builder
    .AddSeq("seq")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("seq")
    .WithEnvironment("ACCEPT_EULA", "Y");

var postgresdb = builder
    .AddPostgres("postgres")
    .WithPgAdmin_MyVersion()
    .WithDataVolume("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("dustypig-v3");

builder
    .AddProject<Projects.DustyPig_Server>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WithReference(seq)
    .WaitFor(seq);

builder.Build().Run();
