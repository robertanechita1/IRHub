using IR_Hub.Data;
using IR_Hub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult New(Vote vot)
        {
            vot.Date_Voted = DateTime.Now;

            // FACUT TIP VOT (LIKE DISLIKE)

            if (ModelState.IsValid)
            {
                db.Votes.Add(vot);
                db.SaveChanges();
                return Redirect("/Bookmars/Show/" + vot.BookmarkId);
            }

            else
            {
                return Redirect("/Bookmarks/Show/" + vot.BookmarkId);
            }

        }




        // Stergerea unui vot asociat unui articol din baza de date
        // Se poate sterge comentariul doar de catre userii cu rolul de Admin 
        // sau de catre utilizatorii cu rolul de User doar daca acel comentariu a fost postat de catre acestia

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Delete(int id)
        {
            Vote vot = db.Votes.Find(id);

            if (vot.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Votes.Remove(vot);
                db.SaveChanges();
                return Redirect("/Bookmarks/Show/" + vot.BookmarkId);
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa anulati votul";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Bookmarks");
            }
        }

    }
}
