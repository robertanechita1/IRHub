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

        //adaugarea unui comentariu dar nu se foloseste

        [HttpGet]
        [Authorize(Roles = "User,Admin")]
        public IActionResult New(int bookmarkId)
        {
            var comment = new Comment
            {
                BookmarkId = bookmarkId
            };
            return View(comment);
        }

       
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

                TempData["message"] = "Comentariul a fost adăugat!";
                TempData["messageType"] = "alert-success";

                return RedirectToAction("New", "Comment");
            }
            else
            {
                return RedirectToAction("New", "Comment");
            }

        }

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

        
        //editarea unui comm 
        // [HttpGet] implicit
        

        [Authorize(Roles = "User,Admin")]
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

        //editarea unui comm dupa ce am primit noul continut
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
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
                    TempData["message"] = "Comentariul a fost modificat cu succes!";
                    TempData["messageType"] = "alert-success";
                    return RedirectToAction("Edit", "Comment", id);
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