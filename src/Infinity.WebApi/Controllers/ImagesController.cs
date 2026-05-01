using Infinity.WebApi.Services;
using Infinity.WebApi.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Infinity.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/attractions")]
public class ImagesController : ControllerBase
{
    private static readonly Dictionary<string, string> MimeTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg",  "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png",  "image/png"  },
            { ".webp", "image/webp" },
            { ".gif",  "image/gif"  },
            { ".avif", "image/avif" },
        };

    private readonly IImageService _images;
    private readonly ImageOptions _options;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(
        IImageService images,
        IOptions<ImageOptions> options,
        ILogger<ImagesController> logger)
    {
        _images  = images;
        _options = options.Value;
        _logger  = logger;
    }

    [HttpGet("image/{*filePath}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> GetImage(string filePath, CancellationToken ct)
    {
        if (!IsValidFilePath(filePath))
            return BadRequest(Problem(
                title:  "Invalid file path",
                detail: "The path must not contain '..' or backslashes, and must not be empty.",
                statusCode: StatusCodes.Status400BadRequest));

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (!MimeTypes.TryGetValue(ext, out var mimeType))
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, Problem(
                title:  "Unsupported media type",
                detail: $"Extension '{ext}' is not supported. Supported: {string.Join(", ", MimeTypes.Keys)}",
                statusCode: StatusCodes.Status415UnsupportedMediaType));

        var stream = await _images.GetImageAsync(filePath, ct);
        if (stream is null)
            return NotFound(Problem(
                title:  "Image not found",
                detail: $"No image at '{filePath}' exists in storage.",
                statusCode: StatusCodes.Status404NotFound));

        return File(stream, mimeType, enableRangeProcessing: true);
    }

    [HttpGet("images")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListImages(CancellationToken ct)
    {
        var names = await _images.ListImagesAsync(ct);
        return Ok(names);
    }

    [HttpPost("image/{*filePath}")]
    [ProducesResponseType(typeof(ImageUploadResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(string filePath, CancellationToken ct)
    {
        if (!IsValidFilePath(filePath))
            return BadRequest(Problem(
                title:  "Invalid file path",
                detail: "The path must not contain '..' or backslashes, and must not be empty.",
                statusCode: StatusCodes.Status400BadRequest));

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (!MimeTypes.ContainsKey(ext))
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, Problem(
                title:  "Unsupported media type",
                detail: $"Extension '{ext}' is not supported.",
                statusCode: StatusCodes.Status415UnsupportedMediaType));

        if (Request.ContentLength > _options.MaxFileSizeBytes)
            return StatusCode(StatusCodes.Status413RequestEntityTooLarge, Problem(
                title:  "Payload too large",
                detail: $"Maximum allowed size is {_options.MaxFileSizeBytes / 1024 / 1024} MB.",
                statusCode: StatusCodes.Status413RequestEntityTooLarge));

        var urlFragment = await _images.SaveImageAsync(filePath, Request.Body, ct);

        _logger.LogInformation(
            "Image uploaded: {FilePath} from {RemoteIp}",
            filePath, HttpContext.Connection.RemoteIpAddress);

        return CreatedAtAction(
            nameof(GetImage),
            new { filePath },
            new ImageUploadResult(filePath, urlFragment));
    }

    [HttpDelete("image/{*filePath}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteImage(string filePath, CancellationToken ct)
    {
        if (!IsValidFilePath(filePath))
            return BadRequest(Problem(
                title:  "Invalid file path",
                detail: "The path must not contain '..' or backslashes, and must not be empty.",
                statusCode: StatusCodes.Status400BadRequest));

        await _images.DeleteImageAsync(filePath, ct);
        return NoContent();
    }
    
    private static bool IsValidFilePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (path.Contains('\\') || path.Contains("..") || path.StartsWith('/'))
            return false;

        return Path.GetFileName(path).Length > 0;
    }
}

public sealed record ImageUploadResult(string FilePath, string Url);