// https://github.com/dotnet/aspire/issues/4359


namespace DustyPig.AppHost;

public class FileStore : Resource
{
    public string SourcePath { get; }

    public FileStore(string name, string? sourcePath = null) : base(name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.AsSpan().ContainsAny(Path.GetInvalidFileNameChars())) throw new ArgumentException("Name contains chars not allowed in folder names", nameof(name));

        if (string.IsNullOrEmpty(sourcePath))
            SourcePath = Directory.CreateTempSubdirectory(name).FullName;
        else if (Path.IsPathRooted(sourcePath))
            throw new NotSupportedException("Use relative paths");
        else
            SourcePath = sourcePath;
    }

    internal string RealHostPath<T>(IResourceBuilder<T> builder) where T : IResourceWithEnvironment =>
       Path.IsPathRooted(SourcePath) ? SourcePath : Path.Combine(builder.ApplicationBuilder.AppHostDirectory, SourcePath);
}

public static class FileStoreExtensions
{
    public static IResourceBuilder<FileStore> AddFileStore(this IDistributedApplicationBuilder builder, [ResourceName] string name, string? sourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var store = new FileStore(name, sourcePath);

        return builder.AddResource(store);
    }

    public static IResourceBuilder<T> WithFileStore<T>(this IResourceBuilder<T> builder, IResourceBuilder<FileStore> store, string? containerTargetMountLocation = null) where T : ContainerResource
    {
        var resource = store.Resource;
        var resourceContainerPath = containerTargetMountLocation ?? $"/appdata/{resource.Name}";
        var containerMountAnnotation = new ContainerMountAnnotation(resource.RealHostPath(builder), resourceContainerPath, ContainerMountType.BindMount, false);
        return builder.WithAnnotation(containerMountAnnotation)
                      .WithEnvironment(context => context.EnvironmentVariables[$"ConnectionStrings__{resource.Name}"] = resourceContainerPath);
    }

    public static IResourceBuilder<T> WithFileStore<T>(this IResourceBuilder<T> builder, IResourceBuilder<FileStore> store) where T : ProjectResource
    {
        var resource = store.Resource;
        return builder.WithEnvironment(context => context.EnvironmentVariables[$"ConnectionStrings__{resource.Name}"] = resource.RealHostPath(builder));
    }
}