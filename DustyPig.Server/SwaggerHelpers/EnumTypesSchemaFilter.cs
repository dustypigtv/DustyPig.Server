using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace DustyPig.Server.SwaggerHelpers
{
    public class EnumTypesSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Enum != null && schema.Enum.Count > 0 && context.Type != null && context.Type.IsEnum)
            {
                if (context.Type == typeof(Ratings))
                    DescribeEnum(schema, context, (s) => s.Replace("_", "-").Replace("NR", "Not Rated"));

                else if (context.Type == typeof(Genres))
                    DescribeEnum(schema, context, (s) => s.Replace("_", " "));

                else if (context.Type == typeof(SortOrder))
                    DescribeEnum(schema, context, (s) => s.Replace("_", " "));

                else
                    DescribeEnum(schema, context, null);
            }
        }


        private void DescribeEnum(OpenApiSchema schema, SchemaFilterContext context, Func<string, string> format)
        {
            int cnt = 0;
            bool flags = context.Type.GetCustomAttributes(true).OfType<FlagsAttribute>().Any();

            if (flags)
            {
                schema.Description += "<p>Possible Bit Flags:</p><ul>";
                int bits = schema.Enum.OfType<OpenApiInteger>().Select(v => v.Value).Count();

                //2 digits per 8 bits
                cnt = Convert.ToInt32(Math.Ceiling((double)bits / 8)) * 2;
            }
            else
            {
                schema.Description += "<p>Possible Values:</p><ul>";
            }

            foreach (var enumIntVal in schema.Enum.OfType<OpenApiInteger>().Select(v => v.Value))
            {
                var enumTypeVal = Enum.Parse(context.Type, enumIntVal.ToString()).ToString();
                if (format != null)
                    enumTypeVal = format(enumTypeVal);

                bool allRatings = context.Type == typeof(Ratings) && enumTypeVal == "All";
                if (!allRatings)
                    if (flags)
                    {
                        string enumHexVal = "0x" + enumIntVal.ToString($"X{cnt}");
                        schema.Description += $"<li>{enumHexVal} = {enumTypeVal}</li>";
                    }
                    else
                    {
                        schema.Description += $"<li>{enumIntVal} = {enumTypeVal}</li>";
                    }
            }
            schema.Description += "</ul>";
        }


    }
}
