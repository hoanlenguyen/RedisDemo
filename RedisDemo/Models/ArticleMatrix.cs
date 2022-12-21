using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RedisDemo.Models
{
    public class ArticleMatrix
    {
        public int Id { get; set; }

        [MaxLength(200)]
        public string? AuthorId { get; set; }

        [MaxLength(200)]
        public string? Author { get; set; }

        [MaxLength(200)]
        public string? Link { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(200)]
        public string? Type { get; set; }

        [MaxLength(200)]
        public string? Category { get; set; }

        [MaxLength(200)]
        public string? Views { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ViewsCount { get; set; }

        public int Likes { get; set; }

        public DateTime PubDate { get; set; }
    }
}