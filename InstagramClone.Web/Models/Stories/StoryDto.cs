using System;

namespace InstagramClone.Web.Models.Stories;

public class StoryDto
{
    public Guid Id { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsMyStory { get; set; }
}
