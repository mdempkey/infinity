namespace Infinity.WebApi.Services;

public interface IImageService
{
    Task<Stream?> GetImageAsync(string fileName, CancellationToken ct = default);

    Task<bool> ImageExistsAsync(string fileName, CancellationToken ct = default);

    Task<string> SaveImageAsync(string fileName, Stream content, CancellationToken ct = default);

    Task DeleteImageAsync(string fileName, CancellationToken ct = default);

    Task<IReadOnlyList<string>> ListImagesAsync(CancellationToken ct = default);
}