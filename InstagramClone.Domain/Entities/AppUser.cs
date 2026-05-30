using InstagramClone.Domain.Common;
using InstagramClone.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Domain.Entities;
public class AppUser: IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FullNameSearch { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; } = AppConstants.DefaultAvatarUrl;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    //
    public bool IsPrivateAccount { get; set; } = false;

    // ReFreshToken
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }

    // Navigation properties
    public ICollection<Post> Posts { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Like> Likes { get; set; } = [];
    
    public ICollection<SavedPost> SavedPosts { get; set; } = [];

    public ICollection<Follow> Followers { get; set; } = [];
    public ICollection<Follow> Followings { get; set; } = [];

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}

