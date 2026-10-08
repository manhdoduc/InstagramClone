using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramClone.Infrastructure.Repositories;

public class ChatRepository(AppDbContext context) : IChatRepository
{
    public async Task<ChatRoom?> GetRoomByIdAsync(Guid id)
    {
        return await context.ChatRooms.FindAsync(id);
    }

    public void AddRoom(ChatRoom room)
    {
        context.ChatRooms.Add(room);
    }

    public async Task<ChatRoom?> GetPrivateRoomAsync(Guid userA, Guid userB)
    {
        return await context.ChatRooms.AsNoTracking()
            .Where(cr => cr.IsGroupChat == false)
            .Where(cr => cr.ChatParticipant.Any(cp => cp.UserId == userA))
            .Where(cr => cr.ChatParticipant.Any(cp => cp.UserId == userB))
            .FirstOrDefaultAsync();
    }

    public async Task<ChatParticipant?> GetParticipantAsync(Guid chatRoomId, Guid userId)
    {
        return await context.ChatParticipants
            .FirstOrDefaultAsync(cp => cp.ChatRoomId == chatRoomId && cp.UserId == userId);
    }

    public async Task<bool> IsParticipantAsync(Guid chatRoomId, Guid userId)
    {
        return await context.ChatParticipants.AnyAsync(cp => cp.ChatRoomId == chatRoomId && cp.UserId == userId);
    }

    public async Task<bool> IsRoomAdminAsync(Guid chatRoomId, Guid userId)
    {
        return await context.ChatParticipants.AsNoTracking()
            .Where(cp => cp.ChatRoomId == chatRoomId && cp.UserId == userId)
            .Select(cp => cp.IsAdmin)
            .FirstOrDefaultAsync();
    }

    public void AddParticipant(ChatParticipant participant)
    {
        context.ChatParticipants.Add(participant);
    }

    public void AddParticipants(IEnumerable<ChatParticipant> participants)
    {
        context.ChatParticipants.AddRange(participants);
    }

    public void RemoveParticipant(ChatParticipant participant)
    {
        context.ChatParticipants.Remove(participant);
    }

    public async Task<int> MarkRoomAsReadAsync(Guid chatRoomId, Guid userId)
    {
        return await context.ChatParticipants
            .Where(m => m.ChatRoomId == chatRoomId && m.UserId == userId)
            .ExecuteUpdateAsync(ex => ex.SetProperty(cp => cp.LastReadAt, DateTime.UtcNow));
    }

    public async Task<Message?> GetMessageByIdAsync(Guid messageId)
    {
        return await context.Messages.FindAsync(messageId);
    }

    public async Task<Message?> GetUserMessageByIdAsync(Guid messageId, Guid userId)
    {
        return await context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);
    }

    public void AddMessage(Message message)
    {
        context.Messages.Add(message);
    }

    public async Task<List<Message>> GetRoomMessagesAsync(Guid chatRoomId, DateTime? cursor, int pageSize)
    {
        IQueryable<Message> query = context.Messages.AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.Reactions)
                .ThenInclude(r => r.User)
            .Where(m => m.ChatRoomId == chatRoomId);

        if (cursor.HasValue)
        {
            query = query.Where(m => m.CreatedAt < cursor.Value);
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(pageSize + 1)
            .ToListAsync();
    }

    public async Task<List<ChatRoom>> GetUserChatRoomsAsync(Guid userId)
    {
        return await context.ChatRooms.AsNoTracking()
            .Include(cr => cr.ChatParticipant)
                .ThenInclude(cp => cp.User)
            .Include(cr => cr.Messages)
            .Where(cr => cr.ChatParticipant.Any(cp => cp.UserId == userId))
            .OrderByDescending(cr => cr.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (DateTime?)m.CreatedAt).FirstOrDefault() ?? cr.CreatedAt)
            .ToListAsync();
    }

    public async Task<MessageReaction?> GetReactionAsync(Guid messageId, Guid userId)
    {
        return await context.MessageReactions
            .FirstOrDefaultAsync(mr => mr.MessageId == messageId && mr.UserId == userId);
    }

    public void AddReaction(MessageReaction reaction)
    {
        context.MessageReactions.Add(reaction);
    }

    public void RemoveReaction(MessageReaction reaction)
    {
        context.MessageReactions.Remove(reaction);
    }
}
