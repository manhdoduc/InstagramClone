using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstagramClone.Domain.Entities;

namespace InstagramClone.Application.DTOs.InfoUser;

public class UserProfileResponseDto
{
    // 1. Thông tin cơ bản
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsPrivateAccount { get; set; }

    // 2. Thống kê (Chỉ lấy con số, tuyệt đối không lấy List Entity)
    public int PostCount { get; set; } = 0;
    public int FollowerCount { get; set; } = 0;
    public int FollowingCount { get; set; } = 0;

    // 3. Trạng thái quan hệ (Dành cho người đang xem profile này)
    // Nếu tự xem trang của mình thì 2 cờ này = false
    public bool IsFollowing { get; set; }
    public bool IsRequested { get; set; } // Nếu account họ private và mình đã gửi yêu cầu

    // 4. Lưới ảnh bài viết (Grid View)
    // Chỉ lấy các thông tin tối thiểu để hiển thị thumbnail, không lấy nội dung text hay comment
    public List<PostGridItemDto> RecentPosts { get; set; } = new();
}

// DTO con chuyên dùng để hiển thị ảnh dạng lưới (Grid) trên trang cá nhân
public class PostGridItemDto
{
    public Guid Id { get; set; }
    // Thường trang cá nhân chỉ hiện ảnh đầu tiên của bài viết làm thumbnail
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
}