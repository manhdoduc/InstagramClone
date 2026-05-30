using AutoMapper;
using InstagramClone.Application.DTOs.InfoUser;
using InstagramClone.Application.DTOs.post;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;

namespace InstagramClone.Application.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        string? currentUserId = null;
        string? targetUserId = null;

        CreateMap<AppUser, UserProfileResponseDto>()
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl ?? ""))
            .ForMember(dest => dest.MyAccount, opt => opt.MapFrom(src => currentUserId == targetUserId))
            .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.Posts.Count()))
            .ForMember(dest => dest.FollowerCount, opt => opt.MapFrom(src => src.Followers.Count(f => f.Status == FollowStatus.Accepted)))
            .ForMember(dest => dest.FollowingCount, opt => opt.MapFrom(src => src.Followings.Count(f => f.Status == FollowStatus.Accepted)))
            .ForMember(dest => dest.IsFollowing, opt => opt.MapFrom(src => !string.IsNullOrEmpty(currentUserId) && src.Followers.Any(f => f.ObserverId == currentUserId && f.Status == FollowStatus.Accepted)))
            .ForMember(dest => dest.IsRequested, opt => opt.MapFrom(src => !string.IsNullOrEmpty(currentUserId) && src.Followers.Any(f => f.ObserverId == currentUserId && f.Status == FollowStatus.Pending)));

        CreateMap<Post, PostGridItemDto>()
            .ForMember(dest => dest.ThumbnailUrl, opt => opt.MapFrom(src => src.MediaItems.OrderBy(m => m.Id).Select(m => m.MediaUrl).FirstOrDefault() ?? ""))
            .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.Likes.Count()))
            .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.Comments.Count()));

        CreateMap<AppUser, UserSummaryDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl ?? ""))
            .ForMember(dest => dest.IsFollowing, opt => opt.MapFrom(src => !string.IsNullOrEmpty(currentUserId) && src.Followers.Any(f => f.ObserverId == currentUserId && f.Status == FollowStatus.Accepted)));
    }
}
