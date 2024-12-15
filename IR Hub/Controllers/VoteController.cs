using IR_Hub.Data;
using IR_Hub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IR_Hub.Controllers
{
    public class VoteController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public VoteController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        // Adaugarea unui vot asociat unui articol in baza de date
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult New(int bookmarkId, string voteType)
        {
            // Obtinem utilizatorul curent
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            // Gasim bookmark-ul asociat
            var bookmark = db.Bookmarks.Include(b => b.Votes).FirstOrDefault(b => b.Id == bookmarkId);
            if (bookmark == null)
            {
                return NotFound();
            }

            // Verificam daca utilizatorul a votat deja
            var existingVote = bookmark.Votes.FirstOrDefault(v => v.UserId == userId);
            if (existingVote != null)
            {
                if (existingVote.Type == voteType)
                {
                    bookmark.VotesCount--;
                    db.Votes.Remove(existingVote);
                    db.SaveChanges();
                    return RedirectToAction("Show", "Bookmark", new { id = bookmarkId });
                }
                else
                {

                    bookmark.VotesCount--;
                    db.Votes.Remove(existingVote);
                    db.SaveChanges();
                }
            }

            var vot = new Vote
            {
                Date_Voted = DateTime.Now,
                UserId = userId,
                Type = voteType == "Like" ? "Like" : "Dislike",
                BookmarkId = bookmarkId
            };
            db.Votes.Add(vot);

            bookmark.VotesCount++;
            db.Entry(bookmark).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Show", "Bookmark", new { id = bookmarkId });
        }


        // Stergerea unui vot asociat unui articol din baza de date

        [HttpPost]
        [Authorize(Roles = "User")]
        public IActionResult Delete(int id)
        {

            var vot = db.Votes.Include(v => v.Bookmark).FirstOrDefault(v => v.Id == id);
            if (vot == null)
            {
                return NotFound();
            }

            if (vot.UserId != _userManager.GetUserId(User))
            {
                TempData["message"] = "Nu aveti dreptul sa anulati votul";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Bookmarks");
            }

            vot.Bookmark.VotesCount--;
            db.Votes.Remove(vot);
            db.SaveChanges();

            return RedirectToAction("Show", "Bookmark", new { id = vot.BookmarkId });
        }

    }
}
