using System;
using System.Collections.Generic;

namespace InstagramClone.Web.Models.Stories;

public class UserStoryFeedDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool HasUnseenStory { get; set; } = true;
    public List<StoryDto> Stories { get; set; } = [];
}
