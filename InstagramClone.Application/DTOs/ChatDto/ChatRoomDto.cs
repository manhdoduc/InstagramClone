using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.DTOs.ChatDto
{
    public class ChatRoomDto
    {
        public Guid Id { get; set; }
        public string? RoomName { get; set; }
        public bool IsGroupChat { get; set; }
        public string? LastestMessage { get; set; } = string.Empty;
        public DateTime? LastestMessageAt { get; set; }
        public int UnreadMessagesCount { get; set; }
    }
}
