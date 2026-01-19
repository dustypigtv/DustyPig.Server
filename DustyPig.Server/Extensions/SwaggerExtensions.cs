using DustyPig.Server.SwaggerHelpers;
using DustyPig.Server.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System;
using System.IO;
using System.Reflection;

namespace DustyPig.Server.Extensions;

internal static class SwaggerExtensions
{
    public static IServiceCollection AddSWagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v3", new OpenApiInfo
            {
                Version = $"v3",
                Title = "Dusty Pig API",
                Description = "API for the Dusty Pig. Each method is marked with a level:<br /><p>" +
                "Requires no authorization: No authentication needed<br />" +
                "Requires account: User must present an account token from Auth/PasswordLogin<br />" +
                "Requires profile: User must present a profile token from auth/profilelogin<br />" +
                "Requires main profile: User must be the main profile on the account</p><br /><br /><p>" +
                $"Server: v{Misc.ServerVersion}<br />" +
                $"Client API: v{DustyPig.API.v3.Client.APIVersion}</p>"
            });

            options.AddSecurityDefinition("Bearer Token", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer", // lowercase per RFC 7235
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

            options.SchemaFilter<EnumTypesSchemaFilter>();

            options.TagActionsBy(api =>
            {
                if (api.GroupName != null)
                    return new[] { api.GroupName };

                if (api.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
                    return new[] { controllerActionDescriptor.ControllerName };

                throw new InvalidOperationException("Unable to determine tag for endpoint.");
            });


            //This is needed to use the [ApiExplorerSettings(GroupName = "...")]
            //https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2745
            options.DocInclusionPredicate((version, desc) => true);
        });


        return services;
    }

    public static IApplicationBuilder UseSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger(options => { options.RouteTemplate = "swagger/{documentName}/swagger.json"; });
        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "swagger";

            //Add and endpiont for each version
            options.SwaggerEndpoint("/swagger/v3/swagger.json", "Dusty Pig API v3");
        });

        return app;
    }

}
