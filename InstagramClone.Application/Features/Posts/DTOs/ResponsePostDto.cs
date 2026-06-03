using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Posts.DTOs{
    public class ResponsePostDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Thông tin ngu?i dang (gom t? b?ng AppUser)
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorAvatar { get; set; }

        // Danh sách URL ?nh (gom t? b?ng PostMedia)
        public List<string> MediaUrls { get; set; } = new List<string>();

        // Thêm 2 thu?c tính này cho ph?n Tuong tác
        public int LikeCount { get; set; }
        public bool IsLiked { get; set; }
        public bool IsSaved { get; set; }

        public int CommentCount { get; set; }
    }
}
