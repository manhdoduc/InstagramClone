using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Posts.DTOs{
    public class ResponseCommentDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorAvatar { get; set; }

        public int LikeCount { get; set; }
        public bool IsLiked { get; set; }
    }
}
