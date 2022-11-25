namespace RedisDemo.Models
{
    public class Feed
    {
        public string Link { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string FeedType { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime PubDate { get; set; } = DateTime.Now;
    }
}