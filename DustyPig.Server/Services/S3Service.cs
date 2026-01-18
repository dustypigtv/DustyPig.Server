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


internal class S3Service : IDisposable
{
    private const string CONFIG_KEY_KEY = "S3-KEY";
    private const string CONFIG_KEY_SECRET = "S3-SECRET";
    private const string CONFIG_KEY_URL = "S3-URL";

    private readonly AmazonS3Client _client;
    private readonly TransferUtility _transferUtility;
    private readonly ILogger<S3Service> _logger;
    
    private bool _disposed;

    public S3Service(IConfiguration configuration, ILogger<S3Service> logger)
    {
        string key = configuration.GetRequiredValue(CONFIG_KEY_KEY);
        string secret = configuration.GetRequiredValue(CONFIG_KEY_SECRET);
        string url = configuration.GetRequiredValue(CONFIG_KEY_URL);
        
        _client = new(key, secret, new AmazonS3Config { ServiceURL = url });
        _transferUtility = new(_client);
        _logger = logger;
    }

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

            await _transferUtility.UploadAsync(req, cancellationToken);
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

            var request = new ListObjectsV2Request { BucketName = Constants.DEFAULT_HOST, Prefix = prefix, Delimiter = "/" };
            ListObjectsV2Response response;
            do
            {
                response = await _client.ListObjectsV2Async(request, cancellationToken).ConfigureAwait(false);

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
            await _client.DeleteObjectAsync(Constants.DEFAULT_HOST, key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(DeleteFileAsync));
            throw;
        }
    }



    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                _client.Dispose();
                _transferUtility.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposed = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~S3Service()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
