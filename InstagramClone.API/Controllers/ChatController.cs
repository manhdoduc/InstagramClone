using InstagramClone.Application.DTOs.ChatDto;
using InstagramClone.Application.DTOs.Chats;
using InstagramClone.Application.DTOs.Common;
using InstagramClone.Application.DTOs.post;
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
        public async Task<ActionResult<CursorPagedResponse<MessageDto>>> GetMessages(Guid roomId, [FromQuery] CursorPaginationRequest messagePagi)
        {
            var result = await chatService.GetMessagesRoomAsync(roomId, messagePagi);
            return ToActionResult(result);
        }

        [HttpGet("rooms")]
        public async Task<ActionResult<List<ChatRoomDto>>> GetUserChatRooms()
        {
            var result = await chatService.GetUserChatRoomsAsync();
            return ToActionResult(result);
        }

        [HttpPost("add-member")]
        public async Task<ActionResult<bool>> AddMemberToGroup(string targetUserId, Guid chatRoomId)
        {
            var result = await chatService.AddMemberToGroupAsync(targetUserId, chatRoomId);
            return ToActionResult(result);
        }

        [HttpPost("leave-group")]
        public async Task<ActionResult<bool>> LeaveGroup(Guid chatRoomId)
        {
            var result = await chatService.LeaveGroupAsync(chatRoomId);
            return ToActionResult(result);
        }

        [HttpPost("remove-member")]
        public async Task<ActionResult<bool>> RemoveMemberFromGroups(string targetUserId, Guid chatRoomId)
        {
            var result = await chatService.RemoveMemberFromGroupAsync(targetUserId, chatRoomId);
            return ToActionResult(result);
        }

        [HttpPost("message")]
        public async Task<ActionResult<MessageDto>> CreateMessage([FromBody] SendMessageDto sendMessage)
        {
            var result = await chatService.CreateMessageAsync(sendMessage);
            return ToActionResult(result);
        }

        [HttpPost("messages/media")]
        public async Task<ActionResult<MessageDto>> UploadImage(IFormFile file, Guid chatRoomId)
        {
            var result = await chatService.UploadMessageMediaAsync(file, chatRoomId);
            return ToActionResult(result);
        }

        [HttpPost("messages/{messageId}/react")]
        public async Task<ActionResult<MessageReaction>> ReactMessage(Guid messageId,  string emoji)
        {
            var result = await chatService.ReactToMessageAsync(messageId, emoji);
            return ToActionResult(result);
        }

        [HttpPost("messages/{messageId}/unsend")]
        public async Task<ActionResult<bool>> UnsendMessage(Guid messageId)
        {
            var result = await chatService.UnsendMessageAsync(messageId);
            return ToActionResult(result);
        }
    }
}