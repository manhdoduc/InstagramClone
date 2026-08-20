namespace InstagramClone.Web.Helpers;

/// <summary>
/// Helper xử lý avatar — fallback khi URL null/rỗng.
/// </summary>
public static class AvatarHelper
{
    private const string DefaultAvatarUrl = "/images/default-avatar.png";

    public static string GetAvatarUrl(string? avatarUrl)
        => string.IsNullOrWhiteSpace(avatarUrl) ? DefaultAvatarUrl : avatarUrl;

    /// <summary>
    /// Lấy initials từ full name để hiển thị avatar placeholder (ví dụ: "Nguyen Van A" → "NA").
    /// </summary>
    public static string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
            : fullName[0].ToString().ToUpper();
    }
}
