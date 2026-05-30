using InstagramClone.Application.DTOs.Common;

namespace InstagramClone.Application.Helpers;

/// <summary>
/// Extracts the repeated cursor-based pagination logic into a reusable helper.
/// Eliminates the duplicated Take+1 → check count → RemoveAt pattern across services.
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Converts a raw list (fetched with pageSize + 1) into a CursorPagedResponse,
    /// trimming the extra item and computing NextCursor automatically.
    /// </summary>
    public static CursorPagedResponse<T> ToCursorPaged<T>(
        List<T> items, int pageSize, Func<T, DateTime> cursorSelector)
    {
        var hasNextPage = items.Count > pageSize;
        DateTime? nextCursor = null;

        if (hasNextPage)
        {
            items.RemoveAt(pageSize);
            nextCursor = cursorSelector(items[^1]); // last item
        }

        return new CursorPagedResponse<T>
        {
            Items = items,
            NextCursor = nextCursor,
            HasNextPage = hasNextPage
        };
    }
}
