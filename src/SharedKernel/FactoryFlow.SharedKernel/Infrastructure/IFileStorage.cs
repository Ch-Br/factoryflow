namespace FactoryFlow.SharedKernel.Infrastructure;

/// <summary>
/// Abstraction for file storage that can be swapped (local disk, S3, Azure Blob, etc.).
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Saves a file and returns the storage key (relative path) for later retrieval.
    /// </summary>
    Task<string> SaveAsync(string directory, string fileName, Stream content, CancellationToken ct = default);

    /// <summary>
    /// Opens a read stream for the given storage key.
    /// </summary>
    Task<Stream> LoadAsync(string storageKey, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a file with the given storage key exists.
    /// </summary>
    Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default);
}
