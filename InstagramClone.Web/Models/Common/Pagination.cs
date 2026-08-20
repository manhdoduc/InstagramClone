namespace InstagramClone.Web.Models.Common;

/// <summary>
/// Mirror của CursorPagedResponse<T> từ backend.
/// Dùng cho tất cả API phân trang cursor-based.
/// </summary>
public class CursorPagedResponse<T>
{
    public List<T> Items { get; set; } = new();

    /// <summary>Điểm neo cho request tiếp theo. Null nếu là trang cuối.</summary>
    public DateTime? NextCursor { get; set; }

    /// <summary>FE dùng để hiện/ẩn nút "Load more" hoặc InfiniteScroll trigger.</summary>
    public bool HasNextPage { get; set; }
}

/// <summary>
/// Query parameters cho cursor pagination, gửi kèm mỗi request.
/// </summary>
public class PaginationRequest
{
    public int PageSize { get; set; } = 10;
    public DateTime? Cursor { get; set; }

    public string ToQueryString()
    {
        var q = $"pageSize={PageSize}";
        if (Cursor.HasValue)
            q += $"&cursor={Cursor.Value:O}"; // ISO 8601
        return q;
    }
}
