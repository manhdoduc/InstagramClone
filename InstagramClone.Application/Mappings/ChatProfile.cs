using AutoMapper;
using InstagramClone.Application.DTOs.ChatDto;
using InstagramClone.Application.DTOs.Chats;
using InstagramClone.Domain.Entities;

namespace InstagramClone.Application.Mappings;

public class ChatProfile : Profile
{
    public ChatProfile()
    {
        string? currentUserId = null;

        CreateMap<MessageReaction, ReactionDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName ?? ""));

        CreateMap<Message, MessageDto>()
            .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender.UserName ?? ""))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.IsDeleted ? "Message unsent" : src.Content))
            .ForMember(dest => dest.MediaUrl, opt => opt.MapFrom(src => src.IsDeleted ? null : src.MediaUrl))
            .ForMember(dest => dest.Reactions, opt => opt.MapFrom(src => src.Reactions));

        CreateMap<ChatRoom, ChatRoomDto>()
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.IsGroupChat ? src.Name : src.ChatParticipant.Where(cp => cp.UserId != currentUserId).Select(cp => cp.User.UserName).FirstOrDefault() ?? "Unknown"))
            .ForMember(dest => dest.LastestMessage, opt => opt.MapFrom(src => src.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault()))
            .ForMember(dest => dest.LastestMessageAt, opt => opt.MapFrom(src => src.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.CreatedAt).FirstOrDefault()))
            .ForMember(dest => dest.UnreadMessagesCount, opt => opt.MapFrom(src => src.Messages.Count(m => m.SenderId != currentUserId && m.CreatedAt > src.ChatParticipant.Where(cp => cp.UserId == currentUserId).Select(cp => cp.LastReadAt).FirstOrDefault())));
    }
}
