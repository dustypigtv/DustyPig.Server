using DustyPig.Server.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace DustyPig.Server.Services;

/// <summary>
/// Wraps Firebase.Auth.Client in a way that DI works correctly with IHttpClientFactory, IConfiguration and ILogger
/// </summary>
public class FirebaseAuthService
    : Firebase.Auth.Client
{
    private const string CONFIG_KEY = "FIREBASE-AUTH-KEY";

    public FirebaseAuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<FirebaseAuthService> logger) :
        base(httpClientFactory.CreateClient(), configuration.GetRequiredValue(CONFIG_KEY), logger)
    { }
}
