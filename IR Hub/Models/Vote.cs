using System.ComponentModel.DataAnnotations.Schema;

namespace IR_Hub.Models
{
    public class Vote
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public int BookmarkId { get; set; }
        [ForeignKey("BookmarkId")]
        public virtual Bookmark Bookmark { get; set; }


        public DateTime Date_Voted { get; set; } = DateTime.Now;
        public string Type { get; set; }
    }
}
