using System.Diagnostics.CodeAnalysis;

namespace DustyPig.Server.Extensions;

internal static class StringExtensions
{
    public static bool IsNullOrWhiteSpace(this string s) => string.IsNullOrWhiteSpace(s);

    public static bool HasValue([NotNullWhen(true)] this string s) => !string.IsNullOrWhiteSpace(s);
}
