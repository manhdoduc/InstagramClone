using InstagramClone.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Domain.Entities
{
    public class ChatRoom : BaseEntity
    {
        public string? Name { get; set; }
        public bool IsGroupChat { get; set; } = false;

        public virtual ICollection<ChatParticipant> ChatParticipant { get; set; } = [];
        public virtual ICollection<Message> Messages { get; set; } = [];
    }
}
