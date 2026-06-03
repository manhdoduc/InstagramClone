using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; protected set; }
        public bool IsDeleted { get; protected set; } = false;

        public void MarkAsDeleted()
        {
            IsDeleted = true;
        }

        public void Restore()
        {
            IsDeleted = false;
        }

        public void ToggleDeleted()
        {
            IsDeleted = !IsDeleted;
        }
    }
}
