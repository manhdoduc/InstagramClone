using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Features.Users.DTOs;
using InstagramClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InstagramClone.Application.Features.Posts.Mappings;

public static class PostMappingExtensions
{
    public static ResponsePostDto ToResponsePostDto(this Post post, Guid currentUserId)
    {
        return new ResponsePostDto
        {
            Id = post.Id,
            Content = post.Content,
            CreatedAt = post.CreatedAt,
            AuthorId = post.UserId.ToString(),
            AuthorName = post.User?.FullName ?? post.User?.UserName ?? string.Empty,
            AuthorAvatar = post.User?.AvatarUrl,
            MediaUrls = post.MediaItems?.Where(m => !m.IsDeleted).Select(m => m.MediaUrl).ToList() ?? new List<string>(),
            LikeCount = post.Likes?.Count(l => !l.IsDeleted) ?? 0,
            CommentCount = post.Comments?.Count(c => !c.IsDeleted) ?? 0,
            IsLiked = post.Likes?.Any(l => l.UserId == currentUserId && !l.IsDeleted) ?? false,
            IsSaved = post.SavedPosts?.Any(s => s.UserId == currentUserId && !s.IsDeleted) ?? false
        };
    }

    public static List<ResponsePostDto> ToResponsePostDtos(this IEnumerable<Post> posts, Guid currentUserId)
    {
        return posts.Select(p => p.ToResponsePostDto(currentUserId)).ToList();
    }

    public static PostGridItemDto ToPostGridItemDto(this Post post)
    {
        return new PostGridItemDto
        {
            Id = post.Id,
            ThumbnailUrl = post.MediaItems?.FirstOrDefault(m => !m.IsDeleted)?.MediaUrl ?? string.Empty,
            LikeCount = post.Likes?.Count(l => !l.IsDeleted) ?? 0,
            CommentCount = post.Comments?.Count(c => !c.IsDeleted) ?? 0
        };
    }

    public static List<PostGridItemDto> ToPostGridItemDtos(this IEnumerable<Post> posts)
    {
        return posts.Select(p => p.ToPostGridItemDto()).ToList();
    }
}
