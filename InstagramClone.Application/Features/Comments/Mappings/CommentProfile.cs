using AutoMapper;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Domain.Entities;

namespace InstagramClone.Application.Features.Comments.Mappings;

public class CommentProfile : Profile
{
    public CommentProfile()
    {
        Guid? currentUserId = null;

        CreateMap<Comment, ResponseCommentDto>()
            .ForMember(dest => dest.AuthorId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.UserName ?? ""))
            .ForMember(dest => dest.AuthorAvatar, opt => opt.MapFrom(src => src.User.AvatarUrl))
            .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.Likes.Count()))
            .ForMember(dest => dest.IsLiked, opt => opt.MapFrom(src => currentUserId.HasValue && src.Likes.Any(l => l.UserId == currentUserId && !l.IsDeleted)));
    }
}
