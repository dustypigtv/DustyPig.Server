// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using OpenTelemetry.Metrics;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering a PostgreSQL database context in an Aspire application.
/// </summary>
public static partial class AspireEFPostgreSqlExtensions_MyVersion
{
    private const string DefaultConfigSectionName = "Aspire:Npgsql:EntityFrameworkCore:PostgreSQL";
    private const DynamicallyAccessedMemberTypes RequiredByEF = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties;

    /// <summary>
    /// Registers the given <see cref="DbContext" /> as a service in the services provided by the <paramref name="builder"/>.
    /// Enables db context pooling, retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext" /> that needs to be registered.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureDbContextOptions">An optional delegate to configure the <see cref="DbContextOptions"/> for the context.</param>
    /// <remarks>
    /// <para>
    /// Reads the configuration from "Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:{typeof(TContext).Name}" config section, or "Aspire:Npgsql:EntityFrameworkCore:PostgreSQL" if former does not exist.
    /// </para>
    /// <para>
    /// The <see cref="DbContext.OnConfiguring" /> method can then be overridden to configure <see cref="DbContext" /> options.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="NpgsqlEntityFrameworkCorePostgreSQLSettings.ConnectionString"/> is not provided.</exception>
    public static void AddNpgsqlDbContext_MyVersion<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configureSettings = null,
        Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        builder.EnsureDbContextNotRegistered<TContext>();

        var settings = builder.GetDbContextSettings<TContext, NpgsqlEntityFrameworkCorePostgreSQLSettings>(
            DefaultConfigSectionName,
            connectionName,
            (settings, section) => section.Bind(settings)
        );

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        //builder.Services.AddDbContextPool<TContext>(ConfigureDbContext);
        builder.Services.AddDbContextFactory<TContext>(ConfigureDbContext);


        ConfigureInstrumentation<TContext>(builder, settings);

        void ConfigureDbContext(DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            // delay validating the ConnectionString until the DbContext is requested. This ensures an exception doesn't happen until a Logger is established.
            ValidateConnectionString(settings.ConnectionString, connectionName, DefaultConfigSectionName, $"{DefaultConfigSectionName}:{typeof(TContext).Name}", isEfDesignTime: EF.IsDesignTime);

            // We don't register a logger factory, because there is no need to: https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder.useloggerfactory?view=efcore-7.0#remarks
            dbContextOptionsBuilder.UseNpgsql(settings.ConnectionString, builder =>
            {
                // Resiliency:
                // 1. Connection resiliency automatically retries failed database commands: https://www.npgsql.org/efcore/misc/other.html#execution-strategy
                if (!settings.DisableRetry)
                {
                    builder.EnableRetryOnFailure();
                }
                // 2. "Scale proportionally: You want to ensure that you don't scale out a resource to a point where it will exhaust other associated resources."
                // The pooling is enabled by default, the min pool size is 0 by default: https://www.npgsql.org/doc/connection-string-parameters.html#pooling
                // There is nothing for us to set here.
                // 3. "Timeout: Places limit on the duration for which a caller can wait for a response."
                // The timeouts have default values, except of Internal Command Timeout, which we should ignore:
                // https://www.npgsql.org/doc/connection-string-parameters.html#timeouts-and-keepalive
                if (settings.CommandTimeout.HasValue)
                {
                    builder.CommandTimeout(settings.CommandTimeout.Value);
                }
            });
            configureDbContextOptions?.Invoke(dbContextOptionsBuilder);
        }
    }

    /// <summary>
    /// Configures retries, health check, logging and telemetry for the <see cref="DbContext" />.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="DbContext"/> is not registered in DI.</exception>
    public static void EnrichNpgsqlDbContext_MyVersion<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
            this IHostApplicationBuilder builder,
            Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configureSettings = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = builder.GetDbContextSettings<TContext, NpgsqlEntityFrameworkCorePostgreSQLSettings>(
            DefaultConfigSectionName,
            null,
            (settings, section) => section.Bind(settings)
        );

        configureSettings?.Invoke(settings);

        ConfigureRetry();

        ConfigureInstrumentation<TContext>(builder, settings);

        void ConfigureRetry()
        {
#pragma warning disable EF1001 // Internal EF Core API usage.
            if (!settings.DisableRetry || settings.CommandTimeout.HasValue)
            {
                builder.CheckDbContextRegistered<TContext>();

#if NET9_0_OR_GREATER
                builder.Services.ConfigureDbContext<TContext>(ConfigureRetryAndTimeout);
#else
                builder.PatchServiceDescriptor<TContext>(ConfigureRetryAndTimeout);
#endif

                void ConfigureRetryAndTimeout(DbContextOptionsBuilder optionsBuilder)
                {
                    optionsBuilder.UseNpgsql(options =>
                    {
                        var extension = optionsBuilder.Options.FindExtension<NpgsqlOptionsExtension>();

                        if (!settings.DisableRetry)
                        {
                            var executionStrategy = extension?.ExecutionStrategyFactory?.Invoke(new ExecutionStrategyDependencies(null!, optionsBuilder.Options, null!));

                            if (executionStrategy != null)
                            {
                                if (executionStrategy is NpgsqlRetryingExecutionStrategy)
                                {
                                    // Keep custom Retry strategy.
                                    // Any sub-class of NpgsqlRetryingExecutionStrategy is a valid retry strategy
                                    // which shouldn't be replaced even with DisableRetry == false
                                }
                                else if (executionStrategy.GetType() != typeof(NpgsqlExecutionStrategy))
                                {
                                    // Check NpgsqlExecutionStrategy specifically (no 'is'), any sub-class is treated as a custom strategy.

                                    throw new InvalidOperationException($"{nameof(NpgsqlEntityFrameworkCorePostgreSQLSettings)}.{nameof(NpgsqlEntityFrameworkCorePostgreSQLSettings.DisableRetry)} needs to be set when a custom Execution Strategy is configured.");
                                }
                                else
                                {
                                    options.EnableRetryOnFailure();
                                }
                            }
                            else
                            {
                                options.EnableRetryOnFailure();
                            }
                        }

                        if (settings.CommandTimeout.HasValue)
                        {
                            if (extension != null &&
                                extension.CommandTimeout.HasValue &&
                                extension.CommandTimeout != settings.CommandTimeout)
                            {
                                throw new InvalidOperationException($"Conflicting values for 'CommandTimeout' were found in {nameof(NpgsqlEntityFrameworkCorePostgreSQLSettings)} and set in DbContextOptions<{typeof(TContext).Name}>.");
                            }

                            options.CommandTimeout(settings.CommandTimeout);
                        }
                    });
                }
#pragma warning restore EF1001 // Internal EF Core API usage.
            }
        }
    }

    private static void ConfigureInstrumentation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(IHostApplicationBuilder builder, NpgsqlEntityFrameworkCorePostgreSQLSettings settings) where TContext : DbContext
    {
        if (!settings.DisableHealthChecks)
        {
            // calling MapHealthChecks is the responsibility of the app, not Component
            builder.TryAddHealthCheck(
                name: typeof(TContext).Name,
                static hcBuilder => hcBuilder.AddDbContextCheck<TContext>());
        }

        if (!settings.DisableTracing)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder.AddNpgsql();
                });
        }

        if (!settings.DisableMetrics)
        {
            builder.Services.AddOpenTelemetry()
                .WithMetrics(AddNpgsqlMetrics);
        }
    }


    private static void EnsureDbContextNotRegistered<TContext>(this IHostApplicationBuilder builder, [CallerMemberName] string callerMemberName = "") where TContext : DbContext
    {
        if (builder.Environment.IsDevelopment())
        {
            ServiceDescriptor serviceDescriptor = builder.Services.FirstOrDefault((ServiceDescriptor sd) => sd.ServiceType == typeof(DbContextOptions<TContext>));
            if (serviceDescriptor != null)
            {
                throw new InvalidOperationException($"DbContext<{typeof(TContext).Name}> is already registered. Please ensure 'services.AddDbContext<{typeof(TContext).Name}>()' is not used when calling '{callerMemberName}()' or use the corresponding 'Enrich' method.");
            }
        }
    }

    /// <summary>
    /// Ensures a <see cref="DbContext"/> is registered in DI.
    /// </summary>
    private static ServiceDescriptor CheckDbContextRegistered<TContext>(this IHostApplicationBuilder builder, [CallerMemberName] string memberName = "")
        where TContext : DbContext
    {
        // Resolving DbContext<TContextService> will resolve DbContextOptions<TContextImplementation>.
        // We need to replace the DbContextOptions service descriptor to inject more logic. This won't be necessary once
        // Aspire targets .NET 9 as EF will respect the calls to services.ConfigureDbContext<TContext>(). c.f. https://github.com/dotnet/efcore/pull/32518

        var oldDbContextOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TContext>));

        if (oldDbContextOptionsDescriptor is null)
        {
            throw new InvalidOperationException($"DbContext<{typeof(TContext).Name}> was not registered. Ensure you have registered the DbContext in DI before calling {memberName}.");
        }

        return oldDbContextOptionsDescriptor;
    }

    private static TSettings GetDbContextSettings<TContext, TSettings>(this IHostApplicationBuilder builder, string defaultConfigSectionName, string? connectionName, Action<TSettings, IConfiguration> bindSettings)
        where TSettings : new()
    {
        TSettings settings = new();
        var configurationSection = builder.Configuration.GetSection(defaultConfigSectionName);
        bindSettings(settings, configurationSection);
        // If the connectionName is not provided, we've been called in the context
        // of an Enrich invocation and don't need to bind the connectionName specific settings.
        // Instead, we'll just bind to the TContext-specific settings.
        if (connectionName is not null)
        {
            var connectionSpecificConfigurationSection = configurationSection.GetSection(connectionName);
            bindSettings(settings, connectionSpecificConfigurationSection);
        }
        var typeSpecificConfigurationSection = configurationSection.GetSection(typeof(TContext).Name);
        if (typeSpecificConfigurationSection.Exists()) // https://github.com/dotnet/runtime/issues/91380
        {
            bindSettings(settings, typeSpecificConfigurationSection);
        }

        return settings;
    }

    private static void ValidateConnectionString(string? connectionString, string connectionName, string defaultConfigSectionName, string? typeSpecificSectionName = null, bool isEfDesignTime = false)
    {
        if (string.IsNullOrWhiteSpace(connectionString) && !isEfDesignTime)
        {
            var errorMessage = (!string.IsNullOrEmpty(typeSpecificSectionName))
                ? $"ConnectionString is missing. It should be provided in 'ConnectionStrings:{connectionName}' or under the 'ConnectionString' key in '{defaultConfigSectionName}' or '{typeSpecificSectionName}' configuration section."
                : $"ConnectionString is missing. It should be provided in 'ConnectionStrings:{connectionName}' or under the 'ConnectionString' key in '{defaultConfigSectionName}' configuration section.";

            throw new InvalidOperationException(errorMessage);
        }
    }

    /// <summary>
    /// Adds a HealthCheckRegistration if one hasn't already been added to the builder.
    /// </summary>
    private static void TryAddHealthCheck(this IHostApplicationBuilder builder, HealthCheckRegistration healthCheckRegistration)
    {
        builder.TryAddHealthCheck(healthCheckRegistration.Name, hcBuilder => hcBuilder.Add(healthCheckRegistration));
    }

    /// <summary>
    /// Invokes the <paramref name="addHealthCheck"/> action if the given <paramref name="name"/> hasn't already been added to the builder.
    /// </summary>
    private static void TryAddHealthCheck(this IHostApplicationBuilder builder, string name, Action<IHealthChecksBuilder> addHealthCheck)
    {
        var healthCheckKey = $"Aspire.HealthChecks.{name}";
        if (!builder.Properties.ContainsKey(healthCheckKey))
        {
            builder.Properties[healthCheckKey] = true;
            addHealthCheck(builder.Services.AddHealthChecks());
        }
    }

    private static void AddNpgsqlMetrics(MeterProviderBuilder meterProviderBuilder)
    {
        double[] secondsBuckets = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10];

        // https://github.com/npgsql/npgsql/blob/4c9921de2dfb48fb5a488787fc7422add3553f50/src/Npgsql/MetricsReporter.cs#L48
        meterProviderBuilder
            .AddMeter("Npgsql")
            // Npgsql's histograms are in seconds, not milliseconds.
            .AddView("db.client.commands.duration",
                new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = secondsBuckets
                })
            .AddView("db.client.connections.create_time",
                new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = secondsBuckets
                });
    }
}
