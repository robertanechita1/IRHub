using IR_Hub.Data;
using IR_Hub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IR_Hub.Controllers
{
    public class CommentsController : Controller
    {
        // PASUL 10 din useri si roluri 

        private readonly ApplicationDbContext db;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public CommentsController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        // Adaugarea unui comentariu asociat unui articol in baza de date
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult New(int bookmarkId, string Cont)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Unauthorized();
                }

                var bookmark = db.Bookmarks.Include(b => b.Votes).FirstOrDefault(b => b.Id == bookmarkId);

                var comm = new Comment
                {
                    Date_created = DateTime.Now,
                    Date_updated = DateTime.Now,
                    UserId = userId,
                    Content = Cont,
                    BookmarkId = bookmarkId
                };

                db.Comments.Add(comm);
                db.SaveChanges();
                bookmark.CommentsCount++;
                db.Entry(bookmark).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Show", "Bookmark", new { id = bookmarkId });
            }
            else
            {
                return RedirectToAction("Show", "Bookmark", new { id = bookmarkId });
            }

        }





        // Stergerea unui comentariu asociat unui articol din baza de date
        // Se poate sterge comentariul doar de catre userii cu rolul de Admin 
        // sau de catre utilizatorii cu rolul de User doar daca acel comentariu a fost postat de catre acestia

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Delete(int id)
        {
            Comment comm = db.Comments.Find(id);

            if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Comments.Remove(comm);
                db.SaveChanges();
                return RedirectToAction("Show/Bookmark", new { id = comm.BookmarkId });
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti comentariul";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Bookmarks");
            }
        }

        // In acest moment vom implementa editarea intr-o pagina View separata
        // Se editeaza un comentariu existent
        // Editarea unui comentariu asociat unui articol
        // [HttpGet] - se executa implicit
        // Se poate edita un comentariu doar de catre utilizatorul care a postat comentariul respectiv 
        // Adminii pot edita orice comentariu, chiar daca nu a fost postat de ei

        [Authorize(Roles = "User")]
        public IActionResult Edit(int id)
        {
            Comment comm = db.Comments.Find(id);

            if (comm.UserId == _userManager.GetUserId(User))
            {
                return View(comm);
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa editati comentariul";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Bookmarks");
            }
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        public IActionResult Edit(int id, Comment requestComment)
        {
            Comment comm = db.Comments.Find(id);

            if (comm.UserId == _userManager.GetUserId(User))
            {
                if (ModelState.IsValid)
                {
                    comm.Content = requestComment.Content;
                    comm.Date_updated = requestComment.Date_created;

                    db.SaveChanges();

                    return RedirectToAction("Show", "Bookmark", new { id = requestComment.BookmarkId });
                }
                else
                {
                    return View(requestComment);
                }
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa editati comentariul";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Articles");
            }
        }
    }
}