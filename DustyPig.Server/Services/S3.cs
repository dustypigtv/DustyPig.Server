using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using DustyPig.API.v3.Models;
using DustyPig.Server.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.Services;


internal class S3(IConfiguration configuration, ILogger<S3> logger)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<S3> _logger = logger;

    public async Task UploadImageAsync(Stream ms, string key, CancellationToken cancellationToken)
    {
        try
        {
            var req = new TransferUtilityUploadRequest
            {
                BucketName = Constants.DEFAULT_HOST,
                InputStream = ms,
                Key = key,
                AutoResetStreamPosition = true,
                AutoCloseStream = true,
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true,
            };
            req.Headers.CacheControl = "max-age=10000000";

            using var client = CreateClient();
            using var transferUtility = new TransferUtility(client);
            await transferUtility.UploadAsync(req, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UploadImageAsync));
            throw;
        }
    }


    public async Task<List<S3Object>> ListImagesAsync(string prefix, string[] extensions, CancellationToken cancellationToken)
    {
        try
        {
            var ret = new List<S3Object>();

            using var client = CreateClient();
            
            var request = new ListObjectsV2Request { BucketName = Constants.DEFAULT_HOST, Prefix = prefix, Delimiter = "/" };
            ListObjectsV2Response response;
            do
            {
                response = await client.ListObjectsV2Async(request, cancellationToken).ConfigureAwait(false);

                foreach (var s3Obj in response.S3Objects)
                {
                    foreach (string ext in extensions)
                    {
                        if (s3Obj.Key.ICEndsWith(ext))
                        {
                            ret.Add(s3Obj);
                            break;
                        }
                    }
                }

                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated ?? false);

            return ret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(ListImagesAsync));
            throw;
        }
    }

    public async Task DeleteFileAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateClient();
            await client.DeleteObjectAsync(Constants.DEFAULT_HOST, key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(DeleteFileAsync));
            throw;
        }
    }

    private AmazonS3Client CreateClient()
    {
        string key = _configuration.GetRequiredValue("S3-KEY");
        string secret = _configuration.GetRequiredValue("S3-SECRET");
        string url = _configuration.GetRequiredValue("S3-URL");

        return new AmazonS3Client(key, secret, new AmazonS3Config { ServiceURL = url });
    }
}
