using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace DustyPig.Server.Extensions;

internal static class ConfigurationExtensions
{
    public const string HTTP_CLIENT_NAME = "MyResilientClient";

    public static HttpClient GetResilientClient(this IHttpClientFactory factory) => factory.CreateClient(HTTP_CLIENT_NAME);

    public static HttpClient GetHttpClient(this IServiceProvider sp) => sp.GetRequiredService<IHttpClientFactory>().GetResilientClient();

    public static string GetRequiredValue(this IConfiguration configuraiton, string name)
    {
        var ret = configuraiton[name];
        if (!ret.HasValue())
            throw new Exception("Configuraiton value '" + nameof(name) + "' is missing, null or empty");
        return ret;
    }
}