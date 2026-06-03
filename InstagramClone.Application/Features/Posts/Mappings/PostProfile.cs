using AutoMapper;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Domain.Entities;

namespace InstagramClone.Application.Features.Posts.Mappings;

public class PostProfile : Profile
{
    public PostProfile()
    {
        Guid? currentUserId = null;

        CreateMap<Post, ResponsePostDto>()
            .ForMember(dest => dest.AuthorId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.FullName))
            .ForMember(dest => dest.AuthorAvatar, opt => opt.MapFrom(src => src.User.AvatarUrl))
            .ForMember(dest => dest.MediaUrls, opt => opt.MapFrom(src => src.MediaItems.Where(m => !m.IsDeleted).Select(m => m.MediaUrl)))
            .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.Likes.Count()))
            .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.Comments.Count()))
            .ForMember(dest => dest.IsLiked, opt => opt.MapFrom(src => src.Likes.Any(l => l.UserId == currentUserId && !l.IsDeleted)))
            .ForMember(dest => dest.IsSaved, opt => opt.MapFrom(src => src.SavedPosts.Any(s => s.UserId == currentUserId)));
    }
}
