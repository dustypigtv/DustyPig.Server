using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

namespace DustyPig.Server.Services;

public static class FDB
{
    public static FirestoreDb Service { get; private set; }

    public static void Configure(string credentialsJson)
    {
        var builder = new FirestoreDbBuilder
        {
            GoogleCredential = GoogleCredential.FromJson(credentialsJson),
            ProjectId = "dusty-pig"
        };

        Service = builder.Build();
    }
}
