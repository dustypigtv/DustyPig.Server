using Amazon.S3;
using Amazon.S3.Model;
using DustyPig.API.v3.Models;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Utilities;
using Krypto.WonderDog.Symmetric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static class Utils
    {
        public static string EnsureNotNull(string s) => (s + string.Empty).Trim();

        public static string Coalesce(params string[] values) => values.FirstOrDefault(item => !string.IsNullOrEmpty(item));

        public static string UniqueFriendId(int id1, int id2) => Crypto.HashString(string.Join("+", new List<int> { id1, id2 }.OrderBy(item => item)));

        public static string GetAssetUrl(EncryptedServiceCredential encryptedCred, string url)
        {
            if (encryptedCred == null)
            {
                if (string.IsNullOrWhiteSpace(url))
                    return null;
                return url;
            }

            switch (encryptedCred.CredentialType)
            {
                case ServiceCredentialTypes.S3:
                    var decryptedS3 = encryptedCred.Decrypt<S3Credential>();
                    var client = new AmazonS3Client(decryptedS3.AccessKey, decryptedS3.AccessSecret, new AmazonS3Config { ServiceURL = decryptedS3.Endpoint });
                    var exp = DateTime.UtcNow.AddDays(5);
                    var req = new GetPreSignedUrlRequest
                    {
                        BucketName = url[..url.IndexOf('/')],
                        Key = url[(url.IndexOf('/') + 1)..],
                        Expires = exp
                    };
                    return client.GetPreSignedURL(req);


                case ServiceCredentialTypes.DPFS:
                    var decryptedDPFS = encryptedCred.Decrypt<DPFSCredential>();
                    var aes = SymmetricFactory.CreateAES();
                    var key = new Krypto.WonderDog.Key(decryptedDPFS.Key);
                    string token = "dpfs=" + WebUtility.UrlEncode(aes.Encrypt(key, DateTime.UtcNow.AddDays(5).AddMinutes(1).ToString("O")));                    
                    url += url.Contains('?') ? '&' : '?';
                    return url + token;

                default:
                    throw new NotImplementedException();
            }

        }
    
        public static string EnsureProfilePic(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                string color = new string[] { "blue", "gold", "green", "grey", "red" }[new Random().Next(0, 5)];
                s = $"https://s3.us-central-1.wasabisys.com/dustypig/media/profile_{color}.png";
            }
            return s;
        }

    }
}
