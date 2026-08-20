using Microsoft.AspNetCore.Components.Forms;

namespace InstagramClone.Web.Models.Posts;

/// <summary>Mirror of ResponsePostDto from backend.</summary>
public class PostDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Author info
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }

    // Media
    public List<string> MediaUrls { get; set; } = new();

    // Interactions
    public int LikeCount { get; set; }
    public bool IsLiked { get; set; }
    public bool IsSaved { get; set; }
    public int CommentCount { get; set; }
}

/// <summary>Mirror of CreatePostDto from backend (sent as multipart/form-data).</summary>
public class CreatePostRequest
{
    public string? Content { get; set; }
    /// <summary>IBrowserFile từ Blazor input file, convert sang stream khi upload.</summary>
    public List<IBrowserFile> Files { get; set; } = new();
}

/// <summary>Mirror of UpdatePostDto from backend.</summary>
public class UpdatePostRequest
{
    public string Content { get; set; } = string.Empty;
}

/// <summary>Mirror of ResponseCommentDto from backend.</summary>
public class CommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public int LikeCount { get; set; }
    public bool IsLiked { get; set; }
}

/// <summary>Mirror of CreateCommentDto from backend.</summary>
public class CreateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}
