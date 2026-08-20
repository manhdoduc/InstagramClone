namespace InstagramClone.Web.Models.Common;

/// <summary>
/// Wrapper chuẩn cho tất cả response từ API.
/// Dùng để xử lý error/success một cách nhất quán.
/// </summary>
public class ApiResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }

    public static ApiResult<T> Success(T data) => new() { IsSuccess = true, Data = data, StatusCode = 200 };
    public static ApiResult<T> Failure(string message, int statusCode = 500) => new() { IsSuccess = false, ErrorMessage = message, StatusCode = statusCode };
}

/// <summary>
/// ApiResult không có data (dùng cho DELETE, logout, v.v.)
/// </summary>
public class ApiResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }

    public static ApiResult Success() => new() { IsSuccess = true, StatusCode = 200 };
    public static ApiResult Failure(string message, int statusCode = 500) => new() { IsSuccess = false, ErrorMessage = message, StatusCode = statusCode };
}
