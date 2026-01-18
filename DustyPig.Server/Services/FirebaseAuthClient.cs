using DustyPig.Server.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace DustyPig.Server.Services;

internal class FirebaseAuthClient : Firebase.Auth.Client
{
    private const string CONFIG_KEY = "FIREBASE-AUTH-KEY";

    public FirebaseAuthClient(HttpClient httpClient, IConfiguration configuration, ILogger<FirebaseAuthClient> logger) : base(httpClient, configuration.GetRequiredValue(CONFIG_KEY), logger) { }
}
