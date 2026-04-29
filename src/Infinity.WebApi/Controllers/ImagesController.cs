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

 
    /// Serves the raw binary of a named attraction image.
  
    [HttpGet("image/{fileName}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> GetImage(string fileName, CancellationToken ct)
    {
        if (!IsValidFileName(fileName))
            return BadRequest(Problem(
                title:  "Invalid file name",
                detail: "The file name must not contain path separators or be empty.",
                statusCode: StatusCodes.Status400BadRequest));

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!MimeTypes.TryGetValue(ext, out var mimeType))
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, Problem(
                title:  "Unsupported media type",
                detail: $"Extension '{ext}' is not supported. Supported: {string.Join(", ", MimeTypes.Keys)}",
                statusCode: StatusCodes.Status415UnsupportedMediaType));

        var stream = await _images.GetImageAsync(fileName, ct);
        if (stream is null)
            return NotFound(Problem(
                title:  "Image not found",
                detail: $"No image named '{fileName}' exists in storage.",
                statusCode: StatusCodes.Status404NotFound));

        return File(stream, mimeType, enableRangeProcessing: true);
    }


    /// Returns a list of all image file names currently in storage.

    [HttpGet("images")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListImages(CancellationToken ct)
    {
        var names = await _images.ListImagesAsync(ct);
        return Ok(names);
    }

  
    /// Uploads and stores a new attraction image.
    /// Overwrites an existing image of the same name.
    
    [HttpPost("image/{fileName}")]
    [ProducesResponseType(typeof(ImageUploadResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB framework ceiling
    public async Task<IActionResult> UploadImage(string fileName, CancellationToken ct)
    {
        if (!IsValidFileName(fileName))
            return BadRequest(Problem(
                title:  "Invalid file name",
                detail: "The file name must not contain path separators or be empty.",
                statusCode: StatusCodes.Status400BadRequest));

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
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

        var urlFragment = await _images.SaveImageAsync(fileName, Request.Body, ct);

        _logger.LogInformation(
            "Image uploaded: {FileName} from {RemoteIp}",
            fileName, HttpContext.Connection.RemoteIpAddress);

        return CreatedAtAction(
            nameof(GetImage),
            new { fileName },
            new ImageUploadResult(fileName, urlFragment));
    }

    
    /// Deletes a stored attraction image. Returns 204 whether or not it existed.
   
    [HttpDelete("image/{fileName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteImage(string fileName, CancellationToken ct)
    {
        if (!IsValidFileName(fileName))
            return BadRequest(Problem(
                title:  "Invalid file name",
                detail: "The file name must not contain path separators or be empty.",
                statusCode: StatusCodes.Status400BadRequest));

        await _images.DeleteImageAsync(fileName, ct);
        return NoContent();
    }

    private static bool IsValidFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (name.Contains('/') || name.Contains('\\'))
            return false;

        var bare = Path.GetFileName(name);
        return !string.IsNullOrEmpty(bare) &&
               string.Equals(bare, name, StringComparison.Ordinal);
    }
}

public sealed record ImageUploadResult(string FileName, string Url);
