using System.ComponentModel.DataAnnotations.Schema;

namespace IR_Hub.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool visibility { get; set; }

        [ForeignKey("UserId")]
        public string? UserId { get; set; }
        
//        public User? User { get; set; }

        public DateTime Date_created { get; set; } = DateTime.Now;
        public DateTime Date_updated { get; set; } = DateTime.Now;
        public virtual ICollection<CategoryBookmark>? CategoryBookmarks { get; set; }
    }
}
