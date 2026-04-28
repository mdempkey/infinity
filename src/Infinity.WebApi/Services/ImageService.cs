using Infinity.WebApi.Settings;
using Microsoft.Extensions.Options;

namespace Infinity.WebApi.Services;

public sealed class ImageService : IImageService
{
    private readonly string _rootPath;
    private readonly IReadOnlyList<string> _allowedExtensions;
    private readonly ILogger<ImageService> _logger;

    public ImageService(
        IOptions<ImageOptions> options,
        IWebHostEnvironment env,
        ILogger<ImageService> logger)
    {
        _logger = logger;
        _allowedExtensions = options.Value.AllowedExtensions;

        // Resolve storage root relative to the application's content root when
        // the configured path is not already absolute.
        var configured = options.Value.StoragePath;
        _rootPath = Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(env.ContentRootPath, configured));

        Directory.CreateDirectory(_rootPath);
        _logger.LogInformation("Image storage root: {Root}", _rootPath);
    }
    
    public Task<bool> ImageExistsAsync(string fileName, CancellationToken ct = default)
    {
        if (!TryResolveSafe(fileName, out var fullPath))
            return Task.FromResult(false);

        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<Stream?> GetImageAsync(string fileName, CancellationToken ct = default)
    {
        if (!TryResolveSafe(fileName, out var fullPath) || !File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public async Task<string> SaveImageAsync(
        string fileName,
        Stream content,
        CancellationToken ct = default)
    {
        if (!TryResolveSafe(fileName, out var fullPath))
            throw new ArgumentException($"Invalid image file name: '{fileName}'.", nameof(fileName));

        ValidateExtension(fileName);

        _logger.LogInformation("Saving image {FileName} to {Path}", fileName, fullPath);

        await using var fs = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);

        await content.CopyToAsync(fs, ct);

        // Return the URL fragment callers can persist in Attraction.ImageUrls.
        return $"/api/attractions/image/{Uri.EscapeDataString(fileName)}";
    }

    public Task DeleteImageAsync(string fileName, CancellationToken ct = default)
    {
        if (!TryResolveSafe(fileName, out var fullPath))
            return Task.CompletedTask;

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted image {FileName}", fileName);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListImagesAsync(CancellationToken ct = default)
    {
        var files = Directory
            .EnumerateFiles(_rootPath)
            .Select(Path.GetFileName)
            .Where(n => n is not null)
            .Cast<string>()
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(files);
    }


    private bool TryResolveSafe(string fileName, out string fullPath)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // GetFileName strips any leading directory component ("..", "/", etc.).
        var bare = Path.GetFileName(fileName);
        if (string.IsNullOrEmpty(bare))
            return false;

        var candidate = Path.GetFullPath(Path.Combine(_rootPath, bare));

        // Ensure the resolved path is inside the storage root.
        if (!candidate.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Path traversal attempt detected. fileName='{FileName}' resolved='{Candidate}'",
                fileName, candidate);
            return false;
        }

        fullPath = candidate;
        return true;
    }

    private void ValidateExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(ext))
            throw new NotSupportedException(
                $"File extension '{ext}' is not allowed. Permitted: {string.Join(", ", _allowedExtensions)}");
    }
}
