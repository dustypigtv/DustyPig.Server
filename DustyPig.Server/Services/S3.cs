using Amazon.S3.Transfer;
using Amazon.S3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using DustyPig.API.v3.Models;

namespace DustyPig.Server.Services
{
    static class S3
    {
        static AmazonS3Client _client;
        static TransferUtility _transferUtility;

        public static void Configure(string url, string key, string secret)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentNullException(nameof(secret));

            if (!url.ToLower().StartsWith("http://") && !url.ToLower().StartsWith("https://"))
                url = "https://" + url;

            _client = new AmazonS3Client(key, secret, new AmazonS3Config { ServiceURL = url });
            _transferUtility = new TransferUtility(_client);
        }

        public static Task UploadProfileArtAsync(MemoryStream ms, int id, CancellationToken cancellationToken = default) =>
            UploadFileAsync(ms, $"profile/{id}.jpg", cancellationToken);
        

        public static Task UploadPlaylistArtAsync(MemoryStream ms, string id, CancellationToken cancellationToken = default) =>
            UploadFileAsync(ms, $"playlist/{id}.jpg", cancellationToken);
        

        static Task UploadFileAsync(MemoryStream ms, string key, CancellationToken cancellationToken)
        {
            ms.Seek(0, SeekOrigin.Begin);
            var req = new TransferUtilityUploadRequest
            {
                BucketName = Constants.DEFAULT_HOST,
                InputStream = ms,
                Key = key
            };
            return _transferUtility.UploadAsync(req, cancellationToken);
        }


        public static Task DeleteProfileArtAsync(int id, CancellationToken cancellationToken = default) =>
            _client.DeleteObjectAsync(Constants.DEFAULT_HOST, $"profile/{id}.jpg", cancellationToken);

        public static Task DeletePlaylistArtAsync(string key, CancellationToken cancellationToken = default) =>
            _client.DeleteObjectAsync(Constants.DEFAULT_HOST, key, cancellationToken);
    }
}
