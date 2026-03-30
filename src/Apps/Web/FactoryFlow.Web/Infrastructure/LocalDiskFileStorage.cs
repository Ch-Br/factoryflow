using FactoryFlow.SharedKernel.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace FactoryFlow.Web.Infrastructure;

public sealed class LocalDiskFileStorage : IFileStorage
{
    private readonly string _basePath;

    public LocalDiskFileStorage(IConfiguration configuration)
    {
        _basePath = configuration["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    }

    public async Task<string> SaveAsync(string directory, string fileName, Stream content, CancellationToken ct = default)
    {
        var sanitized = SanitizeFileName(fileName);
        var relativeDir = directory.Replace('\\', '/').Trim('/');
        var storageKey = $"{relativeDir}/{Guid.NewGuid():N}_{sanitized}";

        var fullPath = Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, ct);

        return storageKey;
    }

    public Task<Stream> LoadAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {storageKey}");

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));
        return Task.FromResult(File.Exists(fullPath));
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }
}
