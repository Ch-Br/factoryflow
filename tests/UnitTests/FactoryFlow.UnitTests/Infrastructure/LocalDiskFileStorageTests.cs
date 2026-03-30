using FactoryFlow.Web.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace FactoryFlow.UnitTests.Infrastructure;

public class LocalDiskFileStorageTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalDiskFileStorage _storage;

    public LocalDiskFileStorageTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "factoryflow_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var config = Substitute.For<IConfiguration>();
        config["FileStorage:BasePath"].Returns(_tempDir);
        _storage = new LocalDiskFileStorage(config);
    }

    [Fact]
    public async Task SaveAsync_CreatesFileOnDisk()
    {
        var content = "Hello, World!"u8.ToArray();
        using var stream = new MemoryStream(content);

        var storageKey = await _storage.SaveAsync("test", "hello.txt", stream);

        storageKey.Should().StartWith("test/");
        storageKey.Should().Contain("hello.txt");

        var fullPath = Path.Combine(_tempDir, storageKey.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(fullPath).Should().BeTrue();
        (await File.ReadAllBytesAsync(fullPath)).Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task LoadAsync_ExistingFile_ReturnsStream()
    {
        var content = "Test content"u8.ToArray();
        using var writeStream = new MemoryStream(content);
        var storageKey = await _storage.SaveAsync("test", "read.txt", writeStream);

        await using var readStream = await _storage.LoadAsync(storageKey);
        using var ms = new MemoryStream();
        await readStream.CopyToAsync(ms);

        ms.ToArray().Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task LoadAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var act = async () => await _storage.LoadAsync("nonexistent/file.txt");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExistsAsync_ExistingFile_ReturnsTrue()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var storageKey = await _storage.SaveAsync("test", "exists.txt", stream);

        var exists = await _storage.ExistsAsync(storageKey);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistent_ReturnsFalse()
    {
        var exists = await _storage.ExistsAsync("nonexistent/file.txt");

        exists.Should().BeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
