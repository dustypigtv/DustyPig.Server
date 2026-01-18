using Asp.Versioning;
using DustyPig.Server.Data;
using DustyPig.Server.HostedServices;
using DustyPig.Server.Middleware;
using DustyPig.Server.Services;
using DustyPig.Server.SwaggerHelpers;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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
using System.Text.Json.Serialization;

namespace DustyPig.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;


            //*** Database Connection ***
#if DEBUG
            string connStr = Configuration["MYSQL-SERVER-V3-DEV"];
#else
            string connStr = Configuration["MYSQL-SERVER-V3"];
#endif
            AppDbContext.Configure(connStr);




            const string FIREBASE_JSON_FILE = "/config/firebase.json";



            //*** Firebase Cloud Messaging ***
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(FIREBASE_JSON_FILE)
            });


            //*** Firebase Firestore DB
            FDB.Configure(FIREBASE_JSON_FILE);



            //*** TMDB ***
            TMDBClient.Configure(Configuration["TMDB-API-KEY"]);



            



            //*** Configure Logging ***
            var config = new LoggingConfiguration();
            var nullTarget = new NullTarget("null");
            config.AddTarget(nullTarget);
            config.LoggingRules.Add(new LoggingRule("Microsoft.*", LogLevel.Trace, LogLevel.Info, nullTarget) { Final = true });
            config.LoggingRules.Add(new LoggingRule("Microsoft.EntityFrameworkCore.*", LogLevel.Trace, LogLevel.Warn, nullTarget) { Final = true });
            config.LoggingRules.Add(new LoggingRule("System.*", LogLevel.Trace, LogLevel.Info, nullTarget) { Final = true });
            config.LoggingRules.Add(new LoggingRule("Microsoft.AspNetCore.DataProtection.*", LogLevel.Trace, nullTarget) { Final = true });


            var dbTarget = new DatabaseTarget("DB")
            {
                DBProvider = "MySql.Data.MySqlClient.MySqlConnection, MySql.Data",
                ConnectionString = connStr,
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

            var ignoreOpCancelledRule = new LoggingRule("Microsoft.EntityFrameworkCore.Query", LogLevel.Error, dbTarget);
            ignoreOpCancelledRule.Filters.Add(new NLog.Filters.ConditionBasedFilter()
            {
                Action = NLog.Filters.FilterResult.IgnoreFinal,
                Condition = "contains('${message}', 'System.OperationCanceledException')"
            });
            config.LoggingRules.Add(ignoreOpCancelledRule);

            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, dbTarget) { Final = true });

            LogManager.Configuration = config;
        }




        public IConfiguration Configuration { get; }



        


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            //Docker health checks
            services.AddHealthChecks();

            services.AddHttpClient();

            // Upload size
            services.Configure<FormOptions>(options =>
            {
                options.KeyLengthLimit = 5242880; //5MB
            });


            //*** CORS ***
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    policy =>
                    {
                        policy.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });




            //*** Memory Caching ***
            services.AddMemoryCache();




            //*** DB Context ***
            services.AddDbContext<AppDbContext>();

            


            //*** Authentication and Authorization ***
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
                        ValidIssuer = JWTService.ISSUER,
                        ValidateIssuer = true,
                        ValidAudience = JWTService.AUDIENCE,
                        ValidateAudience = true,
                        IssuerSigningKey = JWTService.GetSecurityKey(Configuration),
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = false
                    };
                });

            services.AddAuthorization();




            //*** Controller ***
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });


            //*** API Versioning ***
            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(3, 0);
            });



            //*** Routing ***
            services.AddRouting();



            //*******************************
            // SWAGGER
            //*******************************
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v3", new OpenApiInfo
                {
                    Version = $"v3",
                    Title = "Dusty Pig API",
                    Description = "API for the Dusty Pig. Each method is marked with a level:<br /><p>" +
                    "Requires no authorization: No authentication needed<br />" +
                    "Requires account: User must present an account token from either Auth/LoginWithFirebaseToken or Auth/PasswordLogin<br />" +
                    "Requires profile: User must present a profile token from auth/profilelogin<br />" +
                    "Requires main profile: User must be the main profile on the account</p><br /><br /><p>" +
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

                options.OperationFilter<AuthorizeCheckOperationFilter>();


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



            //*** Pages ***
            services.AddControllersWithViews();
            services.AddRazorPages();

            //Raw data
            services.AddMvc(o => o.InputFormatters.Insert(0, new RawRequestBodyFormatter()));


            //*** Dependency Injection ***
            services.AddScoped<FirebaseAuthClient>();
            services.AddScoped<JWTService>();
            services.AddHostedService<TMDB_Updater>();
            services.AddHostedService<FirebaseNotificationsManager>();
            services.AddHostedService<FirestoreMediaChangedTriggerManager>();
            services.AddHostedService<DBCleaner>();
            services.AddHostedService<ArtworkUpdater>();

            services.AddTransient<S3Service>();
        }





        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            AppDbContext.Migrate(app.ApplicationServices);

            app.UseHealthChecks("/health");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //else
            //{
            //    //Staging & Production
            //    app.UseForwardedHeaders(new ForwardedHeadersOptions
            //    {
            //        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            //    });

            //    app.UseHsts();
            //}

            //app.UseHttpsRedirection();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot/.well-known")),
                RequestPath = new PathString("/.well-known"),
                ServeUnknownFileTypes = true // serve extensionless file
            });

            app.UseRouting();

            app.UseCors();

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
                endpoints.MapRazorPages();
            });
        }
    }
}
