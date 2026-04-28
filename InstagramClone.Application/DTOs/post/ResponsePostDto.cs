using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.DTOs.post
{
    public class ResponsePostDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Thông tin người đăng (gom từ bảng AppUser)
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorAvatar { get; set; }

        // Danh sách URL ảnh (gom từ bảng PostMedia)
        public List<string> MediaUrls { get; set; } = new List<string>();

        // Thêm 2 thuộc tính này cho phần Tương tác
        public int LikeCount { get; set; }
        public bool IsLiked { get; set; }
        public bool IsSaved { get; set; }

        public int CommentCount { get; set; }
    }
}
