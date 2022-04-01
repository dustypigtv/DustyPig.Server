namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
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
}
