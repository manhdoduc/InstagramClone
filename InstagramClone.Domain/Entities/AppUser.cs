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
public class AppUser
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UserName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? FullNameSearch { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; } = AppConstants.DefaultAvatarUrl;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public bool IsDeleted { get; private set; } = false;

    public bool IsPrivateAccount { get; private set; } = false;

    public string? RefreshToken { get; private set; }
    public DateTime RefreshTokenExpiryTime { get; private set; }

    private readonly List<Post> _posts = [];
    public IReadOnlyCollection<Post> Posts => _posts.AsReadOnly();

    private readonly List<Comment> _comments = [];
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    private readonly List<Like> _likes = [];
    public IReadOnlyCollection<Like> Likes => _likes.AsReadOnly();
    
    private readonly List<SavedPost> _savedPosts = [];
    public IReadOnlyCollection<SavedPost> SavedPosts => _savedPosts.AsReadOnly();

    private readonly List<Follow> _followers = [];
    public IReadOnlyCollection<Follow> Followers => _followers.AsReadOnly();

    private readonly List<Follow> _followings = [];
    public IReadOnlyCollection<Follow> Followings => _followings.AsReadOnly();

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    // Protected parameterless constructor for EF Core
    protected AppUser() { }

    public AppUser(Guid id, string userName, string email, string firstName, string lastName, string fullNameSearch)
    {
        Id = id;
        UserName = userName;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        FullNameSearch = fullNameSearch;
    }

    // Behaviors
    public void UpdateProfile(string firstName, string lastName, string? bio, string? avatarUrl, string fullNameSearch)
    {
        FirstName = firstName;
        LastName = lastName;
        Bio = bio;
        AvatarUrl = avatarUrl;
        FullNameSearch = fullNameSearch;
    }

    public void UpdateAvatarUrl(string url)
    {
        AvatarUrl = url;
    }

    public void UpdateBio(string? bio)
    {
        Bio = bio;
    }

    public void SetAccountPrivacy(bool isPrivate)
    {
        IsPrivateAccount = isPrivate;
    }

    public void UpdateRefreshToken(string? token, DateTime expiryTime)
    {
        RefreshToken = token;
        RefreshTokenExpiryTime = expiryTime;
    }
}

