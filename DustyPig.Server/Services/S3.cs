using Amazon.S3;
using Amazon.S3.Transfer;
using DustyPig.API.v3.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        public static Task UploadFileAsync(Stream ms, string key, CancellationToken cancellationToken)
        {
            ms.Seek(0, SeekOrigin.Begin);
            var req = new TransferUtilityUploadRequest
            {
                BucketName = Constants.DEFAULT_HOST,
                InputStream = ms,
                Key = key,
                AutoCloseStream = false
            };
            return _transferUtility.UploadAsync(req, cancellationToken);
        }


        public static Task UploadAvatarAsync(Stream ms, string key, CancellationToken cancellationToken)
        {
            //Since avatars now have unique filenames, add Cache-Control: max-age=10000000

            ms.Seek(0, SeekOrigin.Begin);
            var req = new TransferUtilityUploadRequest
            {
                BucketName = Constants.DEFAULT_HOST,
                InputStream = ms,
                Key = key,
                AutoCloseStream = false
            };
            req.Headers.CacheControl = "max-age=10000000";
            return _transferUtility.UploadAsync(req, cancellationToken);
        }

        public static Task DeleteFileAsync(string key, CancellationToken cancellationToken) =>
            _client.DeleteObjectAsync(Constants.DEFAULT_HOST, key, cancellationToken);

    }
}
