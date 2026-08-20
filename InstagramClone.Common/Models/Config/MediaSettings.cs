namespace InstagramClone.Common.Models.Config;

public sealed class MediaSettings
{
    public const string SectionName = "MediaSettings";

    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // Default 5MB

    public ImageDimensionSettings Avatar { get; set; } = new() { MaxWidth = 500, MaxHeight = 500 };
    public ImageDimensionSettings Post { get; set; } = new() { MaxWidth = 1080, MaxHeight = 1350 };
    public ImageDimensionSettings ChatImage { get; set; } = new() { MaxWidth = 400, MaxHeight = 400 };
    public ImageDimensionSettings Story { get; set; } = new() { MaxWidth = 1080, MaxHeight = 1920 };
}

public sealed class ImageDimensionSettings
{
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
}
