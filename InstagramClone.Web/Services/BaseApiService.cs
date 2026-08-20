using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InstagramClone.Web.Models.Common;

namespace InstagramClone.Web.Services;

/// <summary>
/// Base service với các HTTP helper methods đã xử lý error/deserialization.
/// Tất cả domain services kế thừa từ đây để tránh lặp code.
/// </summary>
public abstract class BaseApiService(HttpClient httpClient)
{
    protected readonly HttpClient Http = httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected async Task<ApiResult<T>> GetAsync<T>(string url)
    {
        for (int i = 0; i < 3; i++)
        {
            try
            {
                var response = await Http.GetAsync(url);
                return await ParseResponseAsync<T>(response);
            }
            catch (Exception ex) when (i < 2 && ex.Message.Contains("Connection refused"))
            {
                await Task.Delay(1000); // retry after 1s
            }
            catch (Exception ex)
            {
                return ApiResult<T>.Failure($"Lỗi kết nối: {ex.Message}");
            }
        }
        return ApiResult<T>.Failure("Lỗi kết nối: Không thể kết nối tới máy chủ.");
    }

    protected async Task<ApiResult<T>> PostAsync<T>(string url, object? body = null)
    {
        try
        {
            var content = body != null
                ? new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                : null;
            var response = await Http.PostAsync(url, content);
            return await ParseResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure($"Lỗi kết nối: {ex.Message}");
        }
    }

    protected async Task<ApiResult> PostAsync(string url, object? body = null)
    {
        try
        {
            var content = body != null
                ? new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                : null;
            var response = await Http.PostAsync(url, content);
            return await ParseResponseAsync(response);
        }
        catch (Exception ex)
        {
            return ApiResult.Failure($"Lỗi kết nối: {ex.Message}");
        }
    }

    protected async Task<ApiResult<T>> PutAsync<T>(string url, object? body = null)
    {
        try
        {
            var content = body != null
                ? new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                : null;
            var response = await Http.PutAsync(url, content);
            return await ParseResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure($"Lỗi kết nối: {ex.Message}");
        }
    }

    protected async Task<ApiResult> PutAsync(string url, object? body = null)
    {
        try
        {
            var content = body != null
                ? new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                : null;
            var response = await Http.PutAsync(url, content);
            return await ParseResponseAsync(response);
        }
        catch (Exception ex)
        {
            return ApiResult.Failure($"Lỗi kết nối: {ex.Message}");
        }
    }

    protected async Task<ApiResult<T>> DeleteAsync<T>(string url)
    {
        try
        {
            var response = await Http.DeleteAsync(url);
            return await ParseResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure($"Lỗi kết nối: {ex.Message}");
        }
    }

    protected async Task<ApiResult> DeleteAsync(string url)
    {
        try
        {
            var response = await Http.DeleteAsync(url);
            return await ParseResponseAsync(response);
        }
        catch (Exception ex)
        {
            return ApiResult.Failure($"Lỗi kết nối: {ex.Message}");
        }
    }

    /// <summary>Upload multipart/form-data (ảnh, video...).</summary>
    protected async Task<ApiResult<T>> PostFormAsync<T>(string url, MultipartFormDataContent form)
    {
        try
        {
            var response = await Http.PostAsync(url, form);
            return await ParseResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure($"Lỗi upload: {ex.Message}");
        }
    }

    protected async Task<ApiResult<T>> PutFormAsync<T>(string url, MultipartFormDataContent form)
    {
        try
        {
            var response = await Http.PutAsync(url, form);
            return await ParseResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure($"Lỗi upload: {ex.Message}");
        }
    }

    // --- Parsers ---

    private static async Task<ApiResult<T>> ParseResponseAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = TryExtractErrorMessage(json) ?? response.ReasonPhrase ?? "Lỗi không xác định";
            return ApiResult<T>.Failure(errorMsg, (int)response.StatusCode);
        }

        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return ApiResult<T>.Success(default(T)!);
            }
            
            var data = JsonSerializer.Deserialize<T>(json, JsonOptions);
            return ApiResult<T>.Success(data!);
        }
        catch
        {
            return ApiResult<T>.Failure("Không thể đọc dữ liệu từ server");
        }
    }

    private static async Task<ApiResult> ParseResponseAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var errorMsg = TryExtractErrorMessage(json) ?? response.ReasonPhrase ?? "Lỗi không xác định";
            return ApiResult.Failure(errorMsg, (int)response.StatusCode);
        }
        return ApiResult.Success();
    }

    private static string? TryExtractErrorMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            // 1. ProblemDetails standard fields
            if (doc.RootElement.TryGetProperty("detail", out var detail) && !string.IsNullOrWhiteSpace(detail.GetString()))
                return detail.GetString();
            if (doc.RootElement.TryGetProperty("message", out var msg) && !string.IsNullOrWhiteSpace(msg.GetString()))
                return msg.GetString();
            if (doc.RootElement.TryGetProperty("title", out var title) && !string.IsNullOrWhiteSpace(title.GetString()))
                return title.GetString();

            // 2. Validation errors or error arrays
            if (doc.RootElement.TryGetProperty("errors", out var errors))
            {
                if (errors.ValueKind == JsonValueKind.Array)
                {
                    var messages = new List<string>();
                    foreach (var err in errors.EnumerateArray())
                    {
                        if (err.TryGetProperty("description", out var desc))
                            messages.Add(desc.GetString() ?? "");
                        else if (err.ValueKind == JsonValueKind.String)
                            messages.Add(err.GetString() ?? "");
                    }
                    return messages.Count > 0 ? string.Join(", ", messages) : null;
                }
                else if (errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = new List<string>();
                    foreach (var prop in errors.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in prop.Value.EnumerateArray())
                            {
                                messages.Add(item.GetString() ?? "");
                            }
                        }
                    }
                    return messages.Count > 0 ? string.Join(", ", messages) : null;
                }
            }
        }
        catch { }
        return null;
    }
}
