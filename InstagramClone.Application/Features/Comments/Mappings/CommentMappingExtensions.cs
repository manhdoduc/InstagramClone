using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InstagramClone.Application.Features.Comments.Mappings;

public static class CommentMappingExtensions
{
    public static ResponseCommentDto ToResponseCommentDto(this Comment comment, Guid currentUserId)
    {
        return new ResponseCommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            AuthorId = comment.UserId.ToString(),
            AuthorName = comment.User?.UserName ?? string.Empty,
            AuthorAvatar = comment.User?.AvatarUrl,
            LikeCount = comment.Likes?.Count(l => !l.IsDeleted) ?? 0,
            IsLiked = comment.Likes?.Any(l => l.UserId == currentUserId && !l.IsDeleted) ?? false
        };
    }

    public static List<ResponseCommentDto> ToResponseCommentDtos(this IEnumerable<Comment> comments, Guid currentUserId)
    {
        return comments.Select(c => c.ToResponseCommentDto(currentUserId)).ToList();
    }
}
