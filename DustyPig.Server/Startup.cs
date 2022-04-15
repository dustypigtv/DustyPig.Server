using DustyPig.Server.Data;
using DustyPig.Server.HostedServices;
using DustyPig.Server.Services;
using DustyPig.Server.SwaggerHelpers;
using DustyPig.Server.Utilities;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Config;
using NLog.Targets;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DustyPig.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            AppDbContext.Configure(Configuration["mysql-server-v3"]);

            Crypto.Configure(Configuration["encryption-key"]);

            //Use this for messaging
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromJson(Configuration["firebase-config"])
            });

            TMDBClient.Configure(Configuration["tmdb-api-key"]);

            JWTProvider.Configure(Configuration["jwt-key"]);

            //Write logs to sql database
            var config = new LoggingConfiguration();
            var nullTarget = new NullTarget("null");
            config.AddTarget(nullTarget);
            config.LoggingRules.Add(new LoggingRule("Microsoft.*", LogLevel.Trace, LogLevel.Info, nullTarget) { Final = true });
            config.LoggingRules.Add(new LoggingRule("Microsoft.EntityFrameworkCore.*", LogLevel.Trace, LogLevel.Warn, nullTarget) { Final = true });
            config.LoggingRules.Add(new LoggingRule("System.*", LogLevel.Trace, LogLevel.Info, nullTarget) { Final = true });


            var dbTarget = new DatabaseTarget("DB")
            {
                DBProvider = "MySql.Data.MySqlClient.MySqlConnection, MySql.Data",
                ConnectionString = Configuration["mysql-server-v3"],
                CommandText = @"insert into Logs (
                                    Timestamp, Logger, CallSite, Level, Message, Exception
                                ) values (
                                    @Timestamp, @Logger, @CallSite, @Level, @Message, @Exception
                                );"
            };
            dbTarget.Parameters.Add(new DatabaseParameterInfo("@Timestamp", "${date}"));
            dbTarget.Parameters.Add(new DatabaseParameterInfo("@Logger", "${logger}"));
            dbTarget.Parameters.Add(new DatabaseParameterInfo("@CallSite", "${callsite}"));
            dbTarget.Parameters.Add(new DatabaseParameterInfo("@Level", "${level}"));
            dbTarget.Parameters.Add(new DatabaseParameterInfo("@Message", "${message}"));
            dbTarget.Parameters.Add(new DatabaseParameterInfo("@Exception", "${exception:format=tostring}"));
            config.AddTarget("database", dbTarget);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, dbTarget) { Final = true });

            LogManager.Configuration = config;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();

            services.AddDbContext<AppDbContext>();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = JWTProvider.ISSUER,
                        ValidateIssuer = true,
                        ValidAudience = JWTProvider.AUDIENCE,
                        ValidateAudience = true,
                        IssuerSigningKey = JWTProvider.SigningKey,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = false
                    };
                });

            services.AddAuthorization();

            services
                .AddControllers()
                .AddNewtonsoftJson();

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(3, 0);
            });

            //services.AddRouting(options => options.LowercaseUrls = true);
            services.AddRouting();


            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v3", new OpenApiInfo
                {
                    Version = $"v3",
                    Title = "Dusty Pig API - BETA",
                    Description = "API for the Dusty Pig. Each method is marked with a level:<br /><p>" +
                    "Level 0: No authentication needed<br />" +
                    "Level 1: User must present an account token from either auth/oauthlogin or auth/passwordlogin<br />" +
                    "Level 2: User must present a profile token from auth/profilelogin<br />" +
                    "Level 3: User must be the main profile on the account</p><br /><br /><p>" +
                    $"Server: v{Program.ServerVersion}<br />" +
                    $"Client API: v{API.v3.Client.APIVersion}</p>"
                });

                options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Token"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "bearerAuth"
                            },
                            In = ParameterLocation.Header,
                        },
                        Array.Empty<string>()
                    }
                });



                options.OrderActionsBy((desc) =>
                {
                    string grp = desc.GroupName;
                    if (string.IsNullOrWhiteSpace(grp))
                        grp = desc.ActionDescriptor.RouteValues["controller"];

                    return grp + "_" + desc.ActionDescriptor.DisplayName;
                });

                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
                foreach (var referencedAssembly in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
                {
                    if (referencedAssembly.Name.ICStartsWith("DustyPig."))
                    {
                        string xml = Path.Combine(AppContext.BaseDirectory, referencedAssembly.Name + ".xml");
                        if (File.Exists(xml))
                            options.IncludeXmlComments(xml);
                    }
                }

                options.EnableAnnotations();

                options.OperationFilter<RemoveVersionFromParameter>();
                options.DocumentFilter<ReplaceVersionWithExactValueInPath>();
                options.SchemaFilter<EnumTypesSchemaFilter>();

                options.TagActionsBy(api =>
                {
                    if (api.GroupName != null)
                        return new[] { api.GroupName };

                    if (api.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
                        return new[] { controllerActionDescriptor.ControllerName };

                    throw new InvalidOperationException("Unable to determine tag for endpoint.");
                });


                options.DocInclusionPredicate((version, desc) =>
                {
                    if (!desc.TryGetMethodInfo(out var methodInfo))
                        return false;


                    //var versions = methodInfo
                    //   .DeclaringType?
                    //   .GetCustomAttributes(true)
                    //   .OfType<ApiVersionAttribute>()
                    //   .SelectMany(attr => attr.Versions)
                    //   .ToList();


                    //Since I have base controllers that implement the version #, drill down
                    var versions = new List<ApiVersion>();
                    var parentT = methodInfo.DeclaringType;
                    while (parentT != null)
                    {
                        versions.AddRange
                        (
                            parentT
                                .GetCustomAttributes(true)
                                .OfType<ApiVersionAttribute>()
                                .SelectMany(attr => attr.Versions)
                        );

                        parentT = parentT.BaseType;
                    }


                    var maps = methodInfo
                       .GetCustomAttributes(true)
                       .OfType<MapToApiVersionAttribute>()
                       .SelectMany(attr => attr.Versions)
                       .ToList();

                    return versions?.Any(v => $"v{v}" == version) == true && (!maps.Any() || maps.Any(v => $"v{v}" == version));
                });
            });
            services.AddSwaggerGenNewtonsoftSupport();

            services.AddControllersWithViews();

            services.AddScoped<TMDBClient>();
            services.AddScoped<FirebaseAuthClient>();
            services.AddScoped<JWTProvider>();
            services.AddHostedService<PopularityUpdater>();
            services.AddHostedService<FirebaseNotificationsManager>();
            services.AddHostedService<LogCleaner>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

            }
            else
            {
                //Staging & Production
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });

                app.UseHsts();
            }


            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSwagger(options => { options.RouteTemplate = "swagger/{documentName}/swagger.json"; });
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "swagger";

                //Add and endpiont for each version
                options.SwaggerEndpoint("/swagger/v3/swagger.json", "Dusty Pig API v3");
            });


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}