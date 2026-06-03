using InstagramClone.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Domain.Entities
{
    public class CommentLike : BaseEntity
    {
        public Guid UserId { get; private set; }
        public AppUser User { get; private set; } = null!;

        public Guid CommentId { get; private set; }
        public Comment Comment { get; private set; } = null!;

        protected CommentLike() { }

        public CommentLike(Guid userId, Guid commentId)
        {
            UserId = userId;
            CommentId = commentId;
        }
    }
}
