using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace DustyPig.Server.SwaggerHelpers;

public class EnumTypesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {

    }

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Enum != null && schema.Enum.Count > 0 && context.Type != null && context.Type.IsEnum)
        {
            if (context.Type == typeof(TVRatings))
                DescribeEnum(schema, context, (s) => s.Replace("_", "-").Replace("NotRated", "Not Rated"));

            else if (context.Type == typeof(Genres))
                DescribeEnum(schema, context, (s) => s.Replace("_", " "));

            else if (context.Type == typeof(SortOrder))
                DescribeEnum(schema, context, (s) => s.Replace("_", " "));

            else
                DescribeEnum(schema, context, null);
        }
    }

    private void DescribeEnum(IOpenApiSchema schema, SchemaFilterContext context, Func<string, string> format)
    {
        int cnt = 0;
        bool flags = context.Type.GetCustomAttributes(true).OfType<FlagsAttribute>().Any();

        if (flags)
        {
            schema.Description += "<p>Possible Bit Flags:</p><ul>";
            int bits = schema.Enum.OfType<int>().Count();

            //2 digits per 8 bits
            cnt = Convert.ToInt32(Math.Ceiling((double)bits / 8)) * 2;
        }
        else
        {
            schema.Description += "<p>Possible Values:</p><ul>";
        }


        foreach (var jsonNode in schema.Enum)
        {
            var longVal = jsonNode.GetValue<long>();
            var typeVal = Enum.Parse(context.Type, longVal.ToString()).ToString();
            if (format != null)
                typeVal = format(typeVal);

            if (flags)
            {
                string enumHexVal = "0x" + longVal.ToString($"X{cnt}");
                schema.Description += $"<li>{enumHexVal} = {typeVal}</li>";
            }
            else
            {
                schema.Description += $"<li>{longVal} = {typeVal}</li>";
            }
        }
        schema.Description += "</ul>";
    }


}
