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
        public string? Name { get; private set; }
        public bool IsGroupChat { get; private set; } = false;

        private readonly List<ChatParticipant> _chatParticipant = [];
        public virtual IReadOnlyCollection<ChatParticipant> ChatParticipant => _chatParticipant.AsReadOnly();

        private readonly List<Message> _messages = [];
        public virtual IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

        protected ChatRoom() { }

        public ChatRoom(bool isGroupChat, string? name = null)
        {
            IsGroupChat = isGroupChat;
            Name = name;
        }

        public void AddParticipant(ChatParticipant participant)
        {
            _chatParticipant.Add(participant);
        }

        public void AddMessage(Message message)
        {
            _messages.Add(message);
        }
    }
}
