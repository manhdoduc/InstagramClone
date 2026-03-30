using InstagramClone.Domain.Common;

namespace InstagramClone.Domain.Entities
{
    public class PostMedia : BaseEntity
    {
        public string MediaUrl { get; set; } = string.Empty;

        // Nếu sau này bạn cho up cả Video, có thể thêm cột MediaType (enum: Image, Video)

        // Khóa ngoại trỏ ngược lại Post
        public Guid PostId { get; set; }
        public Post Post { get; set; } = null!;
    }
}