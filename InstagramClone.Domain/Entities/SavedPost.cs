using InstagramClone.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Domain.Entities
{
    public class SavedPost : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser AppUser { get; set; } = null!;
        public Guid PostId { get; set; } 
        public Post Post { get; set; } = null!;
    }
}
