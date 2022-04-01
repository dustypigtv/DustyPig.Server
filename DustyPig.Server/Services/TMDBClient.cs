namespace DustyPig.Server.Services
{
    public class TMDBClient : TMDB.Client
    {
        private static string _apiKey;

        public static void Configure(string apiKey) => _apiKey = apiKey;

        public TMDBClient() : base(_apiKey) { }
    }
}
