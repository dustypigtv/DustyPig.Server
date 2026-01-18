using Microsoft.Extensions.Configuration;
using System;

namespace DustyPig.Server.Extensions;

internal static class ConfigurationExtensions
{
    public static string GetRequiredValue(this IConfiguration configuraiton, string name)
    {
        var ret = configuraiton[name];
        if (!ret.HasValue())
            throw new Exception("Configuraiton value '" + nameof(name) + "' is missing, null or empty");
        return ret;
    }
//}