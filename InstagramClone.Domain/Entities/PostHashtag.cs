namespace InstagramClone.Domain.Entities
{
    public class PostHashtag
    {
        public Guid PostId { get; set; }
        public Post Post { get; set; } = null!;

        public Guid HashtagId { get; set; }
        public Hashtag Hashtag { get; set; } = null!;
    }
}