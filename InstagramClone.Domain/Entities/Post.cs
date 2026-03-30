using InstagramClone.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Domain.Entities
{
    public class Post : BaseEntity
    {
        public string Content { get; set; } = string.Empty;

        // Foreign key to AppUser
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        // quan hệ 1-n với PostMedia
        public ICollection<PostMedia> MediaPorts { get; set; } = [];

        // (Để dành cho Phase 3)
        public ICollection<Like> Likes { get; set; } = [];
        public ICollection<Comment> Comments { get; set; } = [];

        public ICollection<PostHashtag> PostHashtags { get; set; } = [];
    }
}
