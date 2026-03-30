using InstagramClone.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Domain.Entities
{
    public class Hashtag : BaseEntity
    {
        public string Name { get; set; } = null!;
        // Navigation property
        public ICollection<PostHashtag> PostHashtags { get; set; } = [];
    }
}
