using InstagramClone.Domain.Common;
using System;

namespace InstagramClone.Domain.Entities;

public class Story : BaseEntity
{
    public string MediaUrl { get; private set; } = string.Empty;
    public string? Caption { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public Guid UserId { get; private set; }
    public AppUser User { get; private set; } = null!;

    protected Story() { }

    public Story(Guid userId, string mediaUrl, string? caption = null)
    {
        UserId = userId;
        MediaUrl = mediaUrl;
        Caption = caption;
        ExpiresAt = DateTime.UtcNow.AddHours(24);
    }

    public void Expire()
    {
        MarkAsDeleted();
    }
}
