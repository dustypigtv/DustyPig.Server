using Microsoft.Extensions.Configuration;

namespace DustyPig.Server;

public class FileStore
{
    public FileStore(string path) => Path = path;

    public string Path { get; }
}