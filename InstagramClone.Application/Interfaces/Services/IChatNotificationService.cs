using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Services;

public interface IChatNotificationService
{
    Task NotifyNewChatRoomAsync(IEnumerable<string> memberIds, Guid roomId, string message);
    Task NotifyNewChatRoomPrivateAsync(string targetUserId, Guid roomId, string message);
}
