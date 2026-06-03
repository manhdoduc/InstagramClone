using InstagramClone.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Domain.Entities
{
    public class MessageReaction : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string Emoji { get; private set; } = string.Empty;
        public Guid MessageId { get; private set; } = Guid.Empty; 

        // Navigation properties
        public virtual Message Message { get; private set; } = null!;
        public virtual AppUser User { get; private set; } = null!;

        protected MessageReaction() { }

        public MessageReaction(Guid messageId, Guid userId, string emoji)
        {
            MessageId = messageId;
            UserId = userId;
            Emoji = emoji;
        }

        public void ChangeEmoji(string newEmoji)
        {
            Emoji = newEmoji;
        }
    }
}
