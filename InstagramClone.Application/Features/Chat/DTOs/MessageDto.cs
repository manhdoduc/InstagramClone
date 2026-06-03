using InstagramClone.Application.Features.Chat.DTOs;
using InstagramClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Chat.DTOs{
    public class MessageDto
    {
        public Guid Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty; // Ti?n cho FE hi?n th? tên ngu?i chat
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
        public string? MediaUrl { get; set; }
        public List<ReactionDto> Reactions { get; set; } = [];

    }
}
