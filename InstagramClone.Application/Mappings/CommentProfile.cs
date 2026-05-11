using AutoMapper;
using InstagramClone.Application.DTOs.post;
using InstagramClone.Domain.Entities;

namespace InstagramClone.Application.Mappings;

public class CommentProfile : Profile
{
    public CommentProfile()
    {
        string? currentUserId = null;

        CreateMap<Comment, ResponseCommentDto>()
            .ForMember(dest => dest.AuthorId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.AppUser.UserName ?? ""))
            .ForMember(dest => dest.AuthorAvatar, opt => opt.MapFrom(src => src.AppUser.AvatarUrl))
            .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.Likes.Count()))
            .ForMember(dest => dest.IsLiked, opt => opt.MapFrom(src => !string.IsNullOrEmpty(currentUserId) && src.Likes.Any(l => l.UserId == currentUserId && !l.IsDeleted)));
    }
}
