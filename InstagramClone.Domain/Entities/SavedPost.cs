using InstagramClone.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Domain.Entities
{
    public class SavedPost : BaseEntity
    {
        public Guid UserId { get; private set; }
        public AppUser User { get; private set; } = null!;
        public Guid PostId { get; private set; } 
        public Post Post { get; private set; } = null!;

        protected SavedPost() { }

        public SavedPost(Guid userId, Guid postId)
        {
            UserId = userId;
            PostId = postId;
        }
    }
}
