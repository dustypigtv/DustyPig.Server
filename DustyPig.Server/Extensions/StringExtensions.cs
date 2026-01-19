using System.Diagnostics.CodeAnalysis;

namespace DustyPig.Server.Extensions;

internal static class StringExtensions
{
    public static bool IsNullOrWhiteSpace(this string s) => string.IsNullOrWhiteSpace(s);

    public static bool HasValue([NotNullWhen(true)] this string s) => !string.IsNullOrWhiteSpace(s);

    public static string EnsureNotNull(this string s) => (s + string.Empty).Trim();

    public static string FixSpaces(this string s)
    {
        s += string.Empty;
        while (s.Contains("  "))
        {
            s = s.Replace("  ", " ");
        }
        return s.Trim();
    }
}
