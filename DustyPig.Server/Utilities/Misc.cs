using DustyPig.API.v3.Models;
using System;
using System.Linq;

namespace DustyPig.Server.Utilities;

public static class Misc
{
    public static string Coalesce(params string[] values) => values.FirstOrDefault(item => !string.IsNullOrEmpty(item));

    public static string EnsureProfilePic(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            var lst = Constants.DefaultProfileImages();
            s = lst[new Random().Next(0, lst.Count)];
        }
        return s;
    }


    public static Version ServerVersion => typeof(Program).Assembly.GetName().Version;

    public static string CacheBuster(string url, long version)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var k = "_cbv";

        var q = new Uri(url).Query;
        var dict = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(q);
        int idx = 0;
        while (dict.ContainsKey(k))
        {
            k = $"_cbv{idx++}";
        }

        return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, k, version.ToString());
    }
}
