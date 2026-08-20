namespace InstagramClone.Web.Helpers;

/// <summary>
/// Helper chuyển đổi thời gian thành dạng tương đối kiểu Instagram (2h, 3d, 1w...).
/// </summary>
public static class DateTimeHelper
{
    public static string ToRelativeTime(DateTime dateTime)
    {
        var diff = DateTime.UtcNow - dateTime.ToUniversalTime();

        return diff.TotalSeconds switch
        {
            < 60 => "vừa xong",
            < 3600 => $"{(int)diff.TotalMinutes}p",
            < 86400 => $"{(int)diff.TotalHours}g",
            < 604800 => $"{(int)diff.TotalDays}n",
            < 2592000 => $"{(int)(diff.TotalDays / 7)}t",
            _ => dateTime.ToString("dd/MM/yyyy")
        };
    }

    public static string ToFullDateTime(DateTime dateTime)
        => dateTime.ToString("HH:mm dd/MM/yyyy");
}
