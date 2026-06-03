using InstagramClone.Domain.Common;
using InstagramClone.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Domain.Entities;
public class Follow : BaseEntity
{
    // ID c?a ngu?i di follow
    public Guid FollowerId { get; private set; }
    public virtual AppUser Follower { get; private set; } = null!;

    // ID c?a ngu?i du?c follow
    public Guid FolloweeId { get; private set; }
    public virtual AppUser Followee { get; private set; } = null!;

    // Tr?ng thái c?a yêu c?u follow (Pending, Accepted, Rejected)
    public FollowStatus Status { get; private set; } = FollowStatus.Pending;

    protected Follow() { }

    public Follow(Guid followerId, Guid followeeId, FollowStatus status = FollowStatus.Pending)
    {
        FollowerId = followerId;
        FolloweeId = followeeId;
        Status = status;
    }

    public void UpdateStatus(FollowStatus status)
    {
        Status = status;
    }
}
