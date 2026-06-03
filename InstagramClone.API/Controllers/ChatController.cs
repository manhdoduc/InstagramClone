using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Chat.DTOs;
using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramClone.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController(IChatService chatService) : BaseApiController
    {
        #region Room Management

        // GET: api/chat/rooms
        [HttpGet("rooms")]
        public async Task<ActionResult<List<ChatRoomDto>>> GetUserChatRooms()
        {
            var result = await chatService.GetUserChatRoomsAsync();
            return ToActionResult(result);
        }

        // POST: api/chat/private-room/123-abc
        [HttpPost("private-room/{targetUserId}")]
        public async Task<ActionResult<Guid>> CreateOrGetPrivateChatRoom([FromRoute] string targetUserId)
        {
            var result = await chatService.GetOrCreatePrivateRoomAsync(targetUserId);
            return ToActionResult(result);
        }

        // POST: api/chat/group-room
        [HttpPost("group-room")]
        public async Task<ActionResult<Guid>> CreateGroupChatRoom([FromBody] CreateGroupDto groupDto)
        {
            var result = await chatService.CreateGroupRoomAsync(groupDto);
            return ToActionResult(result);
        }

        #endregion

        #region Members Management

        // POST: api/chat/rooms/{chatRoomId}/members/{targetUserId}
        [HttpPost("rooms/{chatRoomId:guid}/members/{targetUserId}")]
        public async Task<ActionResult<bool>> AddMemberToGroup([FromRoute] Guid chatRoomId, [FromRoute] string targetUserId)
        {
            var result = await chatService.AddMemberToGroupAsync(targetUserId, chatRoomId);
            return ToActionResult(result);
        }

        // DELETE: api/chat/rooms/{chatRoomId}/members/me
        [HttpDelete("rooms/{chatRoomId:guid}/members/me")]
        public async Task<ActionResult<bool>> LeaveGroup([FromRoute] Guid chatRoomId)
        {
            var result = await chatService.LeaveGroupAsync(chatRoomId);
            return ToActionResult(result);
        }

        // DELETE: api/chat/rooms/{chatRoomId}/members/{targetUserId}
        [HttpDelete("rooms/{chatRoomId:guid}/members/{targetUserId}")]
        public async Task<ActionResult<bool>> RemoveMemberFromGroups([FromRoute] Guid chatRoomId, [FromRoute] string targetUserId)
        {
            var result = await chatService.RemoveMemberFromGroupAsync(targetUserId, chatRoomId);
            return ToActionResult(result);
        }

        #endregion

        #region Message Management

        // GET: api/chat/rooms/{roomId}/messages?PageSize=20...
        [HttpGet("rooms/{roomId:guid}/messages")]
        public async Task<ActionResult<CursorPagedResponse<MessageDto>>> GetMessages(
            [FromRoute] Guid roomId,
            [FromQuery] CursorPaginationRequest messagePagi)
        {
            var result = await chatService.GetMessagesRoomAsync(roomId, messagePagi);
            return ToActionResult(result);
        }

        // POST: api/chat/messages
        [HttpPost("messages")] // Ð?i t? "message" thành s? nhi?u "messages" cho chu?n REST
        public async Task<ActionResult<MessageDto>> CreateMessage([FromBody] SendMessageDto sendMessage)
        {
            var result = await chatService.CreateMessageAsync(sendMessage);
            return ToActionResult(result);
        }

        // POST: api/chat/rooms/{chatRoomId}/media
        // Ðua media v? dúng tài nguyên Room qu?n lý, dùng FromForm d? upload file
        [HttpPost("rooms/{chatRoomId:guid}/media")]
        public async Task<ActionResult<MessageDto>> UploadImage(IFormFile file, [FromRoute] Guid chatRoomId)
        {
            var result = await chatService.UploadMessageMediaAsync(file, chatRoomId);
            return ToActionResult(result);
        }

        // POST: api/chat/messages/{messageId}/react?emoji=??
        [HttpPost("messages/{messageId:guid}/react")]
        public async Task<ActionResult<MessageReaction>> ReactMessage([FromRoute] Guid messageId, [FromQuery] string emoji)
        {
            var result = await chatService.ReactToMessageAsync(messageId, emoji);
            return ToActionResult(result);
        }

        // DELETE: api/chat/messages/{messageId}
        // "Thu h?i tin nh?n" b?n ch?t là xóa tin nh?n dó ? phía hi?n th? công khai => Nên dùng DELETE thay vì POST unsend
        [HttpDelete("messages/{messageId:guid}")]
        public async Task<ActionResult<bool>> UnsendMessage([FromRoute] Guid messageId)
        {
            var result = await chatService.UnsendMessageAsync(messageId);
            return ToActionResult(result);
        }

        #endregion
    }
}