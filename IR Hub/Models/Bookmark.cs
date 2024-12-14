using System.ComponentModel.DataAnnotations.Schema;

namespace IR_Hub.Models
{
    public class Bookmark
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public string Description { get; set; }
        public string Media_Content { get; set; }
        public DateTime Date_created { get; set; } = DateTime.Now;
        public DateTime Date_updated { get; set; } = DateTime.Now;
        public int? VotesCount { get; set; }
        public int? CommentsCount { get; set; }
        public ICollection<Vote>?  Votes { get; set; }
        public ICollection<Comment>? Comments { get; set; }
        public virtual ICollection<CategoryBookmark>? CategoryBookmarks { get; set; }
    }
}
