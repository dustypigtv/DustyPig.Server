using DustyPig.Server.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace DustyPig.Server.Services;

internal class FirebaseAuthService : Firebase.Auth.Client
{
    private const string CONFIG_KEY = "FIREBASE-AUTH-KEY";

    public FirebaseAuthService(HttpClient httpClient, IConfiguration configuration, ILogger<FirebaseAuthService> logger) : base(httpClient, configuration.GetRequiredValue(CONFIG_KEY), logger) { }
}
