using InstagramClone.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Domain.Entities
{
    public class Comment : BaseEntity
    {
        public string Content { get; set; } = string.Empty;

        // Foreign key to AppUser
        public string UserId { get; set; } = string.Empty;
        public AppUser AppUser { get; set; } = null!;

        // Foreign key to Post
        public Guid PostId { get; set; }
        public Post Post { get; set; } = null!;

        public ICollection<CommentLike> Likes { get; set; } = [];
    }
}
