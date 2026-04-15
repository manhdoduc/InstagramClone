using InstagramClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Chats
{
    public interface IChatHub
    {
        Task UserOnline(string userId);
        Task UserOffline(string userId);
        Task ReceiveMessage(Message message);
        Task NewChatRoomCreated(Guid roomId, string creatorName);
       
        Task MessagesRead(string userId, Guid ChatRoomId);
    }
}
