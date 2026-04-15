using InstagramClone.Application.DTOs.ChatDto;
using InstagramClone.Application.DTOs.Chats;
using InstagramClone.Application.DTOs.Common;
using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Data;
using InstagramClone.Infrastructure.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace InstagramClone.API.Controllers
{
    [Authorize]
    [Route("api/chats")]
    public class ChatController(
        IChatService chatService) : BaseApiController
    {
        [HttpPost("private-room/{targetUserId}")]
        public async Task<ActionResult<Guid>> CreateOrGetPrivateChatRoom(string targetUserId)
        {
            var result = await chatService.GetOrCreatePrivateRoomAsync(targetUserId);
            return ToActionResult(result);
        }

        [HttpPost("group-room")]
        public async Task<ActionResult<Guid>> CreateGroupChatRoom([FromBody] CreateGroupDto groupDto)
        {
            var result = await chatService.CreateGroupRoomAsync(groupDto);
            return ToActionResult(result);
        }

        [HttpGet("message")]
        public async Task<ActionResult<List<MessageDto>>> GetMessages(Guid roomId, [FromQuery] CursorPaginationRequestMessage messagePagi)
        {
            var result = await chatService.GetRoomMessagesAsync(roomId, messagePagi);
            return ToActionResult(result);
        }

        [HttpGet("rooms")]
        public async Task<ActionResult<List<ChatRoomDto>>> GetUserChatRooms()
        {
            var result = await chatService.GetUserChatRoomsAsync();
            return ToActionResult(result);
        }
    }
}