using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using System.IO;
using System.Text.Json;

namespace DustyPig.Server.Services;

public static class FDB
{
    public static FirestoreDb Service { get; private set; }

    public static void Configure(string firebaseJsonFile)
    {

        string projectId = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(firebaseJsonFile)).GetProperty("project_id").GetString();

        var builder = new FirestoreDbBuilder
        {
            GoogleCredential = GoogleCredential.FromFile(firebaseJsonFile),
            ProjectId = projectId
        };

        Service = builder.Build();
    }
}
