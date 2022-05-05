using Krypto.WonderDog;
using Krypto.WonderDog.Hashers;
using Krypto.WonderDog.Symmetric;
using System.Text.RegularExpressions;

namespace DustyPig.Server.Utilities
{
    public static class Crypto
    {
        private static readonly ISymmetric _crypto = SymmetricFactory.CreateAES();
        private static Key _cryptoKey;
        private static readonly IHasher _hasher = HasherFactory.CreateSHA512();

        public static void Configure(string key) => _cryptoKey = new Key(key);

        public static string Encrypt(string value) => _crypto.Encrypt(_cryptoKey, value);

        public static string Decrypt(string value) => _crypto.Decrypt(_cryptoKey, value);

        public static string HashString(string value) => _hasher.Hash(value).Replace("-", null);

        public static string NormalizedHash(string title)
        {
            title = (title + string.Empty).Trim();
            title = title.ToLower();
            title = Regex.Replace(title, "[^\\w]", string.Empty);
            return HashString(title);
        }

        public static string HashMovieTitle(string title, int year) => NormalizedHash($"{title}{year}");

        public static string HashEpisode(int seriesId, int season, int episode) => HashString($"{seriesId}.{season}.{episode}");
    }
}
