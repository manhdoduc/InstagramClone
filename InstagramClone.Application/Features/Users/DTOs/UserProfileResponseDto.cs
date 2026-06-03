using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstagramClone.Domain.Entities;

namespace InstagramClone.Application.Features.Users.DTOs;

public class UserProfileResponseDto
{
    // 1. Thông tin co b?n
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsPrivateAccount { get; set; }
    public bool MyAccount { get; set; } // ID c?a ngu?i dang xem trang này, d? xác d?nh tr?ng thái quan h?


    // 2. Th?ng kê (Ch? l?y con s?, tuy?t d?i không l?y List Entity)
    public int PostCount { get; set; } = 0;
    public int FollowerCount { get; set; } = 0;
    public int FollowingCount { get; set; } = 0;

    // 3. Tr?ng thái quan h? (Dành cho ngu?i dang xem profile này)
    // N?u t? xem trang c?a mình thì 2 c? này = false
    public bool IsFollowing { get; set; }
    public bool IsRequested { get; set; } // N?u account h? private và mình dã g?i yêu c?u

    // 4. Lu?i ?nh bài vi?t (Grid View)
    // Ch? l?y các thông tin t?i thi?u d? hi?n th? thumbnail, không l?y n?i dung text hay comment
    public List<PostGridItemDto> RecentPosts { get; set; } = new();
}

// DTO con chuyên dùng d? hi?n th? ?nh d?ng lu?i (Grid) trên trang cá nhân
public class PostGridItemDto
{
    public Guid Id { get; set; }
    // Thu?ng trang cá nhân ch? hi?n ?nh d?u tiên c?a bài vi?t làm thumbnail
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
}