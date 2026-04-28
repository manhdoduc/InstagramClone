using InstagramClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.DTOs.ChatDto
{
    public class SendMessageDto
    {
        public Guid ChatRoomId { get; set; }
        public string? Content { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
        public string? MediaUrl { get; set; }
        public Guid? ReplyToMessageId { get; set; }
    }
}
