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
    // ID của người đi follow
    public string ObserverId { get; set; } = string.Empty;
    public virtual AppUser Observer { get; set; } = null!;

    // ID của người được follow
    public string TargetId { get; set; } = string.Empty;
    public virtual AppUser Target { get; set; } = null!;

    // Trạng thái của yêu cầu follow (Pending, Accepted, Rejected)
    public FollowStatus Status { get; set; } = FollowStatus.Pending;
}
