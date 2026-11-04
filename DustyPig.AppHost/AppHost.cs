using DustyPig.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("docker-compose");


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
    .WaitFor(postgresdb);

builder.Build().Run();
