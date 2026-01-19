namespace DustyPig
{
    public static class TestAccount
    {
        public const int AccountId = -1; //Or this breaks initial writes
        public const int ProfileId = -1;
        public const string FirebaseId = "TEST ACCOUNT";
        public const string Email = API.v3.Clients.AuthClient.TEST_EMAIL;
        public const string Password = API.v3.Clients.AuthClient.TEST_PASSWORD;
        public const string AvatarUrl = DustyPig.API.v3.Models.Constants.DEFAULT_PROFILE_IMAGE_GREY;
        public const string Name = "Test User";
    }
}
