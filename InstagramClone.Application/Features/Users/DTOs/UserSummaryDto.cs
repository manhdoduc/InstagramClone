using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Users.DTOs;

public class UserSummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsFollowing { get; set; } // Ngu?i dang xem có follow ngu?i này không?
}
