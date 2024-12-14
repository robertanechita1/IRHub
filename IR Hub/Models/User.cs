using Microsoft.AspNetCore.Identity;

namespace IR_Hub.Models
{
    public class User : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? About { get; set; }
        public string? Profile_image { get; set; }
        public string? Role { get; set; }
        public ICollection<Bookmark> Bookmarks { get; set; }
        public ICollection<Vote> Votes { get; set; }
        public ICollection<Comment> Comments { get; set; }
    }


}