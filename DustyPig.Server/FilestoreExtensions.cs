using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace DustyPig.Server;

internal static class FileStoreExtensions
{
    public static void AddFileStore(this IHostApplicationBuilder builder, string fileStoreName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileStoreName);
        if (builder.Configuration.GetConnectionString(fileStoreName) is string path)
            builder.Services.AddKeyedSingleton(fileStoreName, new FileStore(path));
        else
            throw new ArgumentException(nameof(fileStoreName) + " not found");
    }

    public static FileStore GetFileStore(this IServiceProvider serviceProvider, string fileStoreName) =>
        serviceProvider.GetRequiredKeyedService<FileStore>(fileStoreName);
}