namespace DustyPig.Server.Services
{
    public sealed class FirebaseAuthClient : Firebase.Auth.Client
    {
        private static string _apiKey;

        public static void Configure(string apiKey) => _apiKey = apiKey;

        public FirebaseAuthClient() : base(_apiKey) { }

    }
}
