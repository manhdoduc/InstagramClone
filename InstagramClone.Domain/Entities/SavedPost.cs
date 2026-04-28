using InstagramClone.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Domain.Entities
{
    public class SavedPost : BaseEntity
    {
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;
        public AppUser AppUser { get; set; } = null!;
        public Guid PostId { get; set; } 
        public Post Post { get; set; } = null!;
    }
}
