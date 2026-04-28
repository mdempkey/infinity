using System.Text;
using Infinity.WebApi.Services;
using Infinity.WebApi.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Infinity.WebApi.Tests;

public sealed class ImageServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly ImageService _sut;

    public ImageServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"infinity_img_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
        _sut = BuildService(_tempRoot);
    }

    public void Dispose() => Directory.Delete(_tempRoot, recursive: true);

    // ── SaveImageAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveImageAsync_WritesFileToDisk()
    {
        var content = Encoding.UTF8.GetBytes("fake-jpeg-bytes");
        await using var stream = new MemoryStream(content);

        await _sut.SaveImageAsync("test.jpg", stream);

        var expected = Path.Combine(_tempRoot, "test.jpg");
        Assert.True(File.Exists(expected));
        Assert.Equal(content, await File.ReadAllBytesAsync(expected));
    }

    [Fact]
    public async Task SaveImageAsync_ReturnsCorrectUrlFragment()
    {
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var url = await _sut.SaveImageAsync("ride.png", stream);

        Assert.Equal("/api/attractions/image/ride.png", url);
    }

    [Fact]
    public async Task SaveImageAsync_ThrowsOnDisallowedExtension()
    {
        await using var stream = new MemoryStream(new byte[] { 1 });

        await Assert.ThrowsAsync<NotSupportedException>(
            () => _sut.SaveImageAsync("malware.exe", stream));
    }

    // ── GetImageAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetImageAsync_ReturnsStream_WhenFileExists()
    {
        var path = Path.Combine(_tempRoot, "existing.jpg");
        await File.WriteAllBytesAsync(path, new byte[] { 0xFF, 0xD8 }); // JPEG magic

        await using var result = await _sut.GetImageAsync("existing.jpg");

        Assert.NotNull(result);
        Assert.True(result!.CanRead);
    }

    [Fact]
    public async Task GetImageAsync_ReturnsNull_WhenFileDoesNotExist()
    {
        var result = await _sut.GetImageAsync("ghost.jpg");
        Assert.Null(result);
    }

    // ── ImageExistsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ImageExistsAsync_ReturnsFalse_ForMissingFile()
    {
        Assert.False(await _sut.ImageExistsAsync("nope.jpg"));
    }

    [Fact]
    public async Task ImageExistsAsync_ReturnsTrue_AfterSave()
    {
        await using var stream = new MemoryStream(new byte[] { 1 });
        await _sut.SaveImageAsync("present.jpg", stream);

        Assert.True(await _sut.ImageExistsAsync("present.jpg"));
    }

    // ── DeleteImageAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteImageAsync_RemovesFile()
    {
        var path = Path.Combine(_tempRoot, "todelete.jpg");
        await File.WriteAllBytesAsync(path, new byte[] { 1 });

        await _sut.DeleteImageAsync("todelete.jpg");

        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task DeleteImageAsync_DoesNotThrow_WhenFileAbsent()
    {
        await _sut.DeleteImageAsync("nonexistent.jpg");
    }

    // ── ListImagesAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListImagesAsync_ReturnsAllStoredFileNames()
    {
        foreach (var name in new[] { "alpha.jpg", "beta.png", "gamma.webp" })
            await File.WriteAllBytesAsync(Path.Combine(_tempRoot, name), new byte[] { 1 });

        var list = await _sut.ListImagesAsync();

        Assert.Contains("alpha.jpg", list);
        Assert.Contains("beta.png", list);
        Assert.Contains("gamma.webp", list);
    }


    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("../../secret")]
    [InlineData("/absolute/path.jpg")]
    [InlineData("sub/dir/file.jpg")]
    public async Task GetImageAsync_ReturnsFalse_ForTraversalAttempts(string malicious)
    {
        var result = await _sut.GetImageAsync(malicious);
        Assert.Null(result);
    }


    private static ImageService BuildService(string storagePath)
    {
        var options = Options.Create(new ImageOptions { StoragePath = storagePath });

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(storagePath);

        return new ImageService(options, envMock.Object, NullLogger<ImageService>.Instance);
    }
}
