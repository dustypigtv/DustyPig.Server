using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DustyPig.Server.Extensions;

internal static class FirebaseAppExtensions
{
    private const string CONFIG_KEY = "firebase-config";

    private static ServiceAccountCredential GetServiceAccountCredential(IConfiguration configuration) =>
        CredentialFactory.FromJson<ServiceAccountCredential>(configuration.GetRequiredValue(CONFIG_KEY));

    public static IHostApplicationBuilder AddFirebaseCloudMessaging(this IHostApplicationBuilder builder)
    {
        var sac = GetServiceAccountCredential(builder.Configuration);
        FirebaseApp.Create(new AppOptions()
        {
            Credential = sac.ToGoogleCredential()
        });


        return builder;
    }

    public static IHostApplicationBuilder AddFirestoreDb(this IHostApplicationBuilder builder)
    {

        builder.Services.AddSingleton<FirestoreDb>(serviceProvider =>
        {
            var sac = GetServiceAccountCredential(builder.Configuration);
            return new FirestoreDbBuilder()
            {
                GoogleCredential = sac.ToGoogleCredential(),
                ProjectId = sac.ProjectId
            }.Build();
        });

        return builder;
    }
}
