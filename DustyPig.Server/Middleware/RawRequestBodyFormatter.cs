//Based on:
//https://weblog.west-wind.com/posts/2017/Sep/14/Accepting-Raw-Request-Body-Content-in-ASPNET-Core-API-Controllers

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System.IO;
using System.Threading.Tasks;

namespace DustyPig.Server.Middleware;

public class RawRequestBodyFormatter : InputFormatter
{
    const string MIME_TYPE_BINARY = "application/octet-stream";

    public RawRequestBodyFormatter() => SupportedMediaTypes.Add(new MediaTypeHeaderValue(MIME_TYPE_BINARY));

    public override bool CanRead(InputFormatterContext context) =>
        context.HttpContext.Request.ContentType == MIME_TYPE_BINARY;

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        if (context.HttpContext.Request.ContentType == MIME_TYPE_BINARY)
        {
            using (var ms = new MemoryStream())
            {
                await context.HttpContext.Request.Body.CopyToAsync(ms);
                var content = ms.ToArray();
                return await InputFormatterResult.SuccessAsync(content);
            }
        }

        return await InputFormatterResult.FailureAsync();
    }
}