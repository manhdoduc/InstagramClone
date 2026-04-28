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
        public string UserId { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public Guid MessageId { get; set; } = Guid.Empty; 

        // Navigation properties
        public virtual Message Message { get; set; } = null!;
        public virtual AppUser User { get; set; } = null!;
    }
}
