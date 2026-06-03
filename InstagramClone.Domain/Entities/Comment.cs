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
        public string Content { get; private set; } = string.Empty;

        // Foreign key to AppUser
        public Guid UserId { get; private set; }
        public AppUser User { get; private set; } = null!;

        // Foreign key to Post
        public Guid PostId { get; private set; }
        public Post Post { get; private set; } = null!;

        private readonly List<CommentLike> _likes = [];
        public IReadOnlyCollection<CommentLike> Likes => _likes.AsReadOnly();

        protected Comment() { }

        public Comment(Guid postId, Guid userId, string content)
        {
            PostId = postId;
            UserId = userId;
            Content = content;
        }

        public void EditContent(string newContent)
        {
            Content = newContent;
        }
    }
}
