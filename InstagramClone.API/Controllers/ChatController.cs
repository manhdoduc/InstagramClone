using InstagramClone.API.Hubs;
using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace InstagramClone.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ChatController(
        AppDbContext context,
        ICurrentUserService currentUser,
        IHubContext<ChatHub, IChatHub> hubContext) : BaseApiController
    {
        [HttpPost("private-room/{targetUserId}")]
        public async Task<ActionResult<Guid>> CreateOrGetPrivateChatRoom(string targetUserId)
        {
            var userId = currentUser.UserId;
            var nameUser = context.Users.Where(u => u.Id == userId).Select(u => u.UserName).FirstOrDefault();

            // Kiểm tra xem đã có phòng chat riêng giữa 2 người chưa
            var existingChatRoom = await context.ChatRooms
                .Where(cr => cr.IsGroupChat == false)
                .Where(cr => cr.ChatParticipant.Any(cp => cp.UserId == userId)) 
                .Where(cr => cr.ChatParticipant.Any(cp => cp.UserId == targetUserId))
                .FirstOrDefaultAsync();

            if (existingChatRoom != null)
            {
                return ToActionResult(Result<Guid>.Success(existingChatRoom.Id), "Tìm thấy phòng cũ"); ;
            }
            // Nếu chưa có, tạo mới
            var newChatRoom = new ChatRoom
            {
                Id = Guid.NewGuid(),
                IsGroupChat = false,
                Name = ""
            };
            context.ChatRooms.Add(newChatRoom);

            context.ChatParticipants.AddRange(new[]
            {
                new ChatParticipant { ChatRoomId = newChatRoom.Id, UserId = userId },
                new ChatParticipant { ChatRoomId = newChatRoom.Id, UserId = targetUserId }
            });

            await context.SaveChangesAsync();

            // Thông báo cho người dùng liên quan về phòng chat mới (nếu cần)
            // Chúng ta dùng Clients.User(targetUserId) để bắn tin cho đúng người đó
            // SignalR sẽ tự tìm tất cả các ConnectionId của User B để gửi
            await hubContext.Clients.User(targetUserId)
                .NewChatRoomCreated(newChatRoom.Id, nameUser!);

            return ToActionResult(Result<Guid>.Success(newChatRoom.Id), "Tạo phòng chat mới thành công");
        }
    }
}
