using Amazon.S3;
using Amazon.S3.Model;
using DustyPig.API.v3.Models;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Utilities;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static class Utils
    {
        public static string EnsureNotNull(string s) => (s + string.Empty).Trim();

        public static string Coalesce(params string[] values) => values.FirstOrDefault(item => !string.IsNullOrEmpty(item));

        public static string UniqueFriendId(int id1, int id2) => Crypto.HashString(string.Join("+", new List<int> { id1, id2 }.OrderBy(item => item)));

        public static StreamingAsset GetAsset(EncryptedServiceCredential encryptedCred, IMemoryCache memoryCache, string url)
        {
            if (encryptedCred == null)
            {
                if (string.IsNullOrWhiteSpace(url))
                    return null;
                return new StreamingAsset { Url = url, AssetType = StreamingAssetType.Public };
            }


            if (memoryCache.TryGetValue(encryptedCred.Id, out object val))
            {
                switch (encryptedCred.CredentialType)
                {
                    case ServiceCredentialTypes.S3:
                        var client = (AmazonS3Client)val;
                        var exp = DateTime.UtcNow.AddDays(5);
                        var req = new GetPreSignedUrlRequest
                        {
                            BucketName = url[..url.IndexOf('/')],
                            Key = url[(url.IndexOf('/') + 1)..],
                            Expires = exp
                        };
                        return new StreamingAsset { Url = client.GetPreSignedURL(req), ExpiresUTC = exp, AssetType = StreamingAssetType.S3 };

                    case ServiceCredentialTypes.GoogleDriveServiceAccount:
                        var token = (API.v3.Models.GoogleDriveToken)val;
                        if (token.ExpiresUTC > DateTime.UtcNow.AddMinutes(15))
                            return new StreamingAsset { Url = url, Token = token.Token, ExpiresUTC = token.ExpiresUTC, AssetType = StreamingAssetType.GoogleDrive };
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            switch (encryptedCred.CredentialType)
            {
                case ServiceCredentialTypes.S3:
                    var decryptedS3 = encryptedCred.Decrypt<S3Credential>();
                    var client = new AmazonS3Client(decryptedS3.AccessKey, decryptedS3.AccessSecret, new AmazonS3Config { ServiceURL = decryptedS3.Endpoint });
                    memoryCache.Set(encryptedCred.Id, client, TimeSpan.FromDays(365));
                    var exp = DateTime.UtcNow.AddDays(5);
                    var req = new GetPreSignedUrlRequest
                    {
                        BucketName = url[..url.IndexOf('/')],
                        Key = url[(url.IndexOf('/') + 1)..],
                        Expires = exp
                    };
                    return new StreamingAsset { Url = client.GetPreSignedURL(req), ExpiresUTC = exp, AssetType = StreamingAssetType.S3 };

                case ServiceCredentialTypes.GoogleDriveServiceAccount:
                    var decryptedGD = encryptedCred.Decrypt<GoogleDriveCredential>();
                    var service = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = GoogleCredential
                            .FromJson(decryptedGD.ServiceCredentialsJson)
                            .CreateScoped(new string[] { DriveService.Scope.Drive })
                            .CreateWithUser(decryptedGD.Email),
                        ApplicationName = "Dusty Pig"
                    });
                    string tokenVal = ((ICredential)service.HttpClientInitializer).GetAccessTokenForRequestAsync().Result;
                    var gdToken = new API.v3.Models.GoogleDriveToken
                    {
                        Token = ((ICredential)service.HttpClientInitializer).GetAccessTokenForRequestAsync().Result,
                        ExpiresUTC = DateTime.UtcNow.AddMinutes(45)
                    };
                    memoryCache.Set(encryptedCred.Id, gdToken, TimeSpan.FromMinutes(45));
                    return new StreamingAsset { Url = url, Token = gdToken.Token, ExpiresUTC = gdToken.ExpiresUTC, AssetType = StreamingAssetType.GoogleDrive };

                default:
                    throw new NotImplementedException();
            }

        }
    }
}
