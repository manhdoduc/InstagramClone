using System;
using System.Collections.Generic;

namespace InstagramClone.Application.Features.Users.DTOs;

public class UserSummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsFollowing { get; set; }
}
