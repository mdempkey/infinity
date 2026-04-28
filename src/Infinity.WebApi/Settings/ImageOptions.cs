namespace Infinity.WebApi.Settings;

public class ImageOptions
{
    public const string SectionName = "Images";

    public string StoragePath { get; set; } = "images/attractions";

    public IReadOnlyList<string> AllowedExtensions { get; set; } =
        [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
}