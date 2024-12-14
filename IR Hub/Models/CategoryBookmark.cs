using IR_Hub.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace IR_Hub.Models
{
    public class CategoryBookmark
    {
        // tabelul asociativ care face legatura intre Category si Bookmark
        // un bookmark are mai multe categorii din care face parte
        // iar o categorie contine mai multe bookmarks in cadrul ei
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        // cheie primara compusa (Id, ArticleId, BookmarkId)
        public int Id { get; set; }
        public int? CategoryId { get; set; }
        public int? BookmarkId { get; set; }

        public virtual Category? Category { get; set; }
        public virtual Bookmark? Bookmark { get; set; }

        public DateTime CategoryCreatedDate { get; set; }

    }
}

