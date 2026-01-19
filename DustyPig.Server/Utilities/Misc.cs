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

}
