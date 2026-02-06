using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DustyPig.Server.SwaggerHelpers;

public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType is null)
            return;

        var hasAuthorize =
            context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
            context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

        if (hasAuthorize)
        {
            var jwtBearerScheme = new OpenApiSecuritySchemeReference("Bearer", context.Document);           
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [jwtBearerScheme] = []
                }
            ];
        }
    }
}
