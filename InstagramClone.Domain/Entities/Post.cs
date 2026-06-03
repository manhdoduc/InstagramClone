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
        public string Content { get; private set; } = string.Empty;

        // Foreign key to AppUser
        public Guid UserId { get; private set; }
        public AppUser User { get; private set; } = null!;

        // quan hệ 1-n với PostMedia
        private readonly List<PostMedia> _mediaItems = [];
        public IReadOnlyCollection<PostMedia> MediaItems => _mediaItems.AsReadOnly();

        // (Để dành cho Phase 3)
        private readonly List<Like> _likes = [];
        public IReadOnlyCollection<Like> Likes => _likes.AsReadOnly();
        
        private readonly List<Comment> _comments = [];
        public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

        private readonly List<PostHashtag> _postHashtags = [];
        public IReadOnlyCollection<PostHashtag> PostHashtags => _postHashtags.AsReadOnly();
        
        private readonly List<SavedPost> _savedPosts = [];
        public IReadOnlyCollection<SavedPost> SavedPosts => _savedPosts.AsReadOnly();

        protected Post() { }

        public Post(Guid userId, string content)
        {
            UserId = userId;
            Content = content;
        }

        public void EditContent(string newContent)
        {
            Content = newContent;
        }

        public void AddMedia(PostMedia media)
        {
            _mediaItems.Add(media);
        }
    }
}
