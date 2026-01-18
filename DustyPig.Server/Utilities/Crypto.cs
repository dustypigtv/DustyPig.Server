using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DustyPig.Server.Utilities;


public static partial class Crypto
{
    private static readonly SHA512 _hasher = SHA512.Create();

    [GeneratedRegex("[^\\w]")]
    private static partial Regex NormalizedHashRegex();


    public static string HashString(string value) => Convert.ToHexString(_hasher.ComputeHash(Encoding.UTF8.GetBytes(value + string.Empty)));

    public static string NormalizedHash(string title)
    {
        title = (title + string.Empty).Trim();
        title = title.ToLower();
        title = NormalizedHashRegex().Replace(title, string.Empty);
        return HashString(title);
    }

    public static string HashMovieTitle(string title, int year) => NormalizedHash($"{title}{year}");

    public static string HashEpisode(int seriesId, int season, int episode) => HashString($"{seriesId}.{season}.{episode}");
}
