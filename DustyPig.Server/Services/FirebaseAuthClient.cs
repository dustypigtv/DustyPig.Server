namespace DustyPig.Server.Services
{
    public sealed class FirebaseAuthClient : Firebase.Auth.Client
    {
        private const string API_KEY = "AIzaSyC_m_o4f1_zU0zpUDW9FYfpZiZG_KMXi8Q";

        public FirebaseAuthClient() : base(API_KEY) { }
        
    }
}
