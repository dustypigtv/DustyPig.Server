using DustyPig.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static class Utils
    {
        public static string EnsureNotNull(string s) => (s + string.Empty).Trim();

        public static string Coalesce(params string[] values) => values.FirstOrDefault(item => !string.IsNullOrEmpty(item));

        public static string UniqueFriendId(int id1, int id2) => Crypto.HashString(string.Join("+", new List<int> { id1, id2 }.OrderBy(item => item)));

        public static string EnsureProfilePic(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                string color = new string[] { "blue", "gold", "green", "grey", "red" }[new Random().Next(0, 5)];
                s = $"https://s3.us-central-1.wasabisys.com/dustypig/media/profile_{color}.png";
            }
            return s;
        }

    }
}
