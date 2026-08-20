using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;
using InstagramClone.Web.Models.Common;
using InstagramClone.Web.Models.Posts;

namespace InstagramClone.Web.Services.Posts;

public class PostService(HttpClient httpClient) : BaseApiService(httpClient), IPostService
{
    public async Task<ApiResult<CursorPagedResponse<PostDto>>> GetFeedAsync(PaginationRequest request)
        => await GetAsync<CursorPagedResponse<PostDto>>($"/api/posts/feed?{request.ToQueryString()}");

    public async Task<ApiResult<PostDto>> GetPostByIdAsync(Guid id)
        => await GetAsync<PostDto>($"/api/posts/{id}");

    public async Task<ApiResult<CursorPagedResponse<PostDto>>> GetUserPostsAsync(string userId, PaginationRequest request)
        => await GetAsync<CursorPagedResponse<PostDto>>($"/api/posts/user/{userId}?{request.ToQueryString()}");

    public async Task<ApiResult<CursorPagedResponse<PostDto>>> GetSavedPostsAsync(PaginationRequest request)
        => await GetAsync<CursorPagedResponse<PostDto>>($"/api/posts/saved-posts?{request.ToQueryString()}");

    public async Task<ApiResult<CursorPagedResponse<PostDto>>> SearchPostsAsync(string content, PaginationRequest request)
        => await GetAsync<CursorPagedResponse<PostDto>>($"/api/posts/search?content={Uri.EscapeDataString(content)}&{request.ToQueryString()}");

    public async Task<ApiResult<string>> CreatePostAsync(string? content, List<IBrowserFile> files)
    {
        using var form = new MultipartFormDataContent();

        if (!string.IsNullOrWhiteSpace(content))
        {
            form.Add(new StringContent(content), "Content");
        }

        foreach (var file in files)
        {
            var memoryStream = new MemoryStream();
            using (var fileStream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024))
            {
                await fileStream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            var streamContent = new StreamContent(memoryStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(streamContent, "Files", file.Name);
        }

        return await PostFormAsync<string>("/api/posts", form);
    }

    public async Task<ApiResult<bool>> UpdatePostAsync(Guid id, string content)
        => await PutAsync<bool>($"/api/posts/{id}", new { Content = content });

    public async Task<ApiResult> DeletePostAsync(Guid id)
        => await DeleteAsync($"/api/posts/{id}");

    public async Task<ApiResult<string>> ToggleLikeAsync(Guid id)
        => await PostAsync<string>($"/api/posts/{id}/like");

    public async Task<ApiResult<string>> ToggleSaveAsync(Guid id)
        => await PostAsync<string>($"/api/posts/{id}/toggle-save");
}
