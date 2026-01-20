using Aspire.Hosting.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;

namespace DustyPig.AppHost;

internal static class PostgresBuilderExtensions
{
    public static IResourceBuilder<T> WithPgAdmin_MyVersion<T>(this IResourceBuilder<T> builder, Action<IResourceBuilder<PgAdminContainerResource>>? configureContainer = null, string? containerName = "pgadmin", string? dataVolumeName = "pgadmin", IResourceBuilder<FileStore>? fileStore = null)
        where T : PostgresServerResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<PgAdminContainerResource>().SingleOrDefault() is { } existingPgAdminResource)
        {
            var builderForExistingResource = builder.ApplicationBuilder.CreateResourceBuilder(existingPgAdminResource);
            configureContainer?.Invoke(builderForExistingResource);
            return builder;
        }
        else
        {
            containerName ??= "pgadmin";
            dataVolumeName ??= containerName;

            //Don't exclude from manifest
            var pgAdminContainer = new PgAdminContainerResource(containerName);
            var pgAdminContainerBuilder = builder.ApplicationBuilder.AddResource(pgAdminContainer)
                                                 .WithImage(PostgresContainerImageTags.PgAdminImage, PostgresContainerImageTags.PgAdminTag)
                                                 .WithImageRegistry(PostgresContainerImageTags.PgAdminRegistry)
                                                 .WithHttpEndpoint(targetPort: 80, name: "http")
                                                 .WithEnvironment(SetPgAdminEnvironmentVariables)
                                                 .WithHttpHealthCheck("/browser")
                                                 .WithLifetime(ContainerLifetime.Persistent)
                                                 .WithVolume(dataVolumeName, "/var/lib/pgadmin", false)
                                                 //.ExcludeFromManifest();
                                                 ;

            //Add fileStore
            if (fileStore != null)
                pgAdminContainerBuilder = pgAdminContainerBuilder.WithFileStore(fileStore, "/db-shared");

            pgAdminContainerBuilder.WithContainerFiles(
                destinationPath: "/pgadmin4",
                callback: async (context, cancellationToken) =>
                {
                    var appModel = context.ServiceProvider.GetRequiredService<DistributedApplicationModel>();
                    var postgresInstances = builder.ApplicationBuilder.Resources.OfType<PostgresServerResource>();

                    return [
                        new ContainerFile
                        {
                            Name = "servers.json",
                            Contents = await WritePgAdminServerJson(postgresInstances, cancellationToken).ConfigureAwait(false),
                        },
                    ];
                });

            configureContainer?.Invoke(pgAdminContainerBuilder);

            pgAdminContainerBuilder.WithRelationship(builder.Resource, "PgAdmin");

            return builder;
        }
    }


    private static async Task<string> WritePgAdminServerJson(IEnumerable<PostgresServerResource> postgresInstances, CancellationToken cancellationToken)
    {
        using MemoryStream stream = new MemoryStream();
        using Utf8JsonWriter writer = new Utf8JsonWriter((Stream)stream, new JsonWriterOptions
        {
            Indented = true
        });
        writer.WriteStartObject();
        writer.WriteStartObject("Servers");
        int serverIndex = 1;
        foreach (PostgresServerResource postgresInstance in postgresInstances)
        {
            EndpointReference endpoint = postgresInstance.PrimaryEndpoint;
            string text = ((postgresInstance.UserNameParameter != null) ? (await postgresInstance.UserNameParameter.GetValueAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) : "postgres");
            string userName = text;
            string text2 = await postgresInstance.PasswordParameter.GetValueAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            writer.WriteStartObject($"{serverIndex}");
            writer.WriteString("Name", postgresInstance.Name);
            writer.WriteString("Group", "Servers");
            writer.WriteString("Host", endpoint.Resource.Name);
            writer.WriteNumber("Port", endpoint.TargetPort.Value);
            writer.WriteString("Username", userName);
            writer.WriteString("SSLMode", "prefer");
            writer.WriteString("MaintenanceDB", "postgres");
            writer.WriteString("PasswordExecCommand", "echo '" + text2 + "'");
            writer.WriteEndObject();
            serverIndex++;
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void SetPgAdminEnvironmentVariables(EnvironmentCallbackContext context)
    {
        // Disables pgAdmin authentication.
        context.EnvironmentVariables["PGADMIN_CONFIG_MASTER_PASSWORD_REQUIRED"] = "False";
        context.EnvironmentVariables["PGADMIN_CONFIG_SERVER_MODE"] = "False";

        // You need to define the PGADMIN_DEFAULT_EMAIL and PGADMIN_DEFAULT_PASSWORD or PGADMIN_DEFAULT_PASSWORD_FILE environment variables.
        context.EnvironmentVariables["PGADMIN_DEFAULT_EMAIL"] = "admin@domain.com";
        context.EnvironmentVariables["PGADMIN_DEFAULT_PASSWORD"] = "admin";

        // When running in the context of Codespaces we need to set some additional environment
        // variables so that PGAdmin will trust the forwarded headers that Codespaces port
        // forwarding will send.
        var config = context.ExecutionContext.ServiceProvider.GetRequiredService<IConfiguration>();
        if (context.ExecutionContext.IsRunMode && config.GetValue<bool>("CODESPACES", false))
        {
            context.EnvironmentVariables["PGADMIN_CONFIG_PROXY_X_HOST_COUNT"] = "1";
            context.EnvironmentVariables["PGADMIN_CONFIG_PROXY_X_PREFIX_COUNT"] = "1";
        }
    }
}
