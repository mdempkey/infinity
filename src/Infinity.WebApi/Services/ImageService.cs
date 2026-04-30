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

        var configured = options.Value.StoragePath;
        _rootPath = Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(env.ContentRootPath, configured));

        Directory.CreateDirectory(_rootPath);
        _logger.LogInformation("Image storage root: {Root}", _rootPath);
    }

    public Task<bool> ImageExistsAsync(string filePath, CancellationToken ct = default)
    {
        if (!TryResolveSafe(filePath, out var fullPath))
            return Task.FromResult(false);

        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<Stream?> GetImageAsync(string filePath, CancellationToken ct = default)
    {
        if (!TryResolveSafe(filePath, out var fullPath) || !File.Exists(fullPath))
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
        string filePath,
        Stream content,
        CancellationToken ct = default)
    {
        if (!TryResolveSafe(filePath, out var fullPath))
            throw new ArgumentException($"Invalid image path: '{filePath}'.", nameof(filePath));

        ValidateExtension(filePath);

        var dir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);

        _logger.LogInformation("Saving image {FilePath} to {FullPath}", filePath, fullPath);

        await using var fs = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);

        await content.CopyToAsync(fs, ct);

        return $"/api/attractions/image/{filePath.Replace('\\', '/')}";
    }

    public Task DeleteImageAsync(string filePath, CancellationToken ct = default)
    {
        if (!TryResolveSafe(filePath, out var fullPath))
            return Task.CompletedTask;

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted image {FilePath}", filePath);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListImagesAsync(CancellationToken ct = default)
    {
        var files = Directory
            .EnumerateFiles(_rootPath, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(_rootPath, f).Replace('\\', '/'))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(files);
    }

    private bool TryResolveSafe(string filePath, out string fullPath)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        if (filePath.Contains('\\') || filePath.Contains("..") || filePath.StartsWith('/'))
        {
            _logger.LogWarning(
                "Unsafe path rejected: '{FilePath}'", filePath);
            return false;
        }

        var candidate = Path.GetFullPath(Path.Combine(_rootPath, filePath));

        var root = _rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Path traversal attempt: '{FilePath}' resolved to '{Candidate}'",
                filePath, candidate);
            return false;
        }

        fullPath = candidate;
        return true;
    }

    private void ValidateExtension(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (!_allowedExtensions.Contains(ext))
            throw new NotSupportedException(
                $"File extension '{ext}' is not allowed. Permitted: {string.Join(", ", _allowedExtensions)}");
    }
}