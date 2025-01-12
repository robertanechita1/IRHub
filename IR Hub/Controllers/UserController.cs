using IR_Hub.Models;
using IR_Hub.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArticlesApp.Controllers;

//[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly ApplicationDbContext db;

    private readonly UserManager<User> _userManager;

    private readonly SignInManager<User> _signInManager; //am nevoie pentru delogare in momentul stergerii contului

    public UserController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        SignInManager<User> signInManager
        )
    {
        db = context;

        _userManager = userManager;

        _signInManager = signInManager;
    }
    public IActionResult Index()
    {
        var users = from user in db.Users
                    orderby user.UserName
                    select user;

        ViewBag.UsersList = users;

        return View();
    }

    public IActionResult Show(string id)
    {
        var userId = _userManager.GetUserId(User);
        var user = db.Users.FirstOrDefault(u => u.Id == id);

        if (TempData.ContainsKey("message"))
        {
            ViewBag.Msg = TempData["message"].ToString();
        }

        if (user == null)
        {
            return NotFound();
        }

        SetAccessRights(id);

        /*// un URL implicit pentru poza de profil, daca nu exista
        user.Profile_image = string.IsNullOrEmpty(user.Profile_image)
            ? "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ9eLeSj523CsBb4S2TNM4BZ-8TuObk0YsoaFQVvATuYEGEXLqxjIqAxOJh0z2xgU1kPzc&usqp=CAU"
            : user.Profile_image;*/


        if (userId == id || User.IsInRole("Admin")) 
        {
            var UserCategories = db.Categories.Where(u => u.UserId == id);
            ViewBag.UserCategories = UserCategories;

            return View(user);
        }
        else
        {
            var UserCategories = db.Categories.Where(u => u.UserId == id && u.visibility == true);
            ViewBag.UserCategories = UserCategories;

            return View(user);
        }

        
        
    }
    [HttpGet]

    [HttpGet]
    public IActionResult Edit(string id)
    {
        var user = db.Users.Find(id);

        if (user == null)
        {
            return NotFound(); 
        }

        return View(user);
    }


    [HttpPost]
    public IActionResult Edit(string id, User newData)
    {
        if (ModelState.IsValid)
        {
            var user = db.Users.Find(id);

            if (user == null)
            {
                return NotFound(); 
            }

            if (string.IsNullOrWhiteSpace(newData.FirstName) && string.IsNullOrWhiteSpace(newData.LastName))
            {
                user.FirstName = "Utilizator";
                user.LastName = "Necunoscut";
            }
            else if (string.IsNullOrWhiteSpace(newData.FirstName))
            {
                user.FirstName = " ";
                user.LastName = newData.LastName;
            }
            else if (string.IsNullOrWhiteSpace(newData.LastName))
            {
                user.LastName = " ";
                user.FirstName = newData.FirstName;
            }
            else
            {
                user.FirstName = newData.FirstName;
                user.LastName = newData.LastName;
            }

            user.UserName = newData.UserName;
            user.Profile_image = newData.Profile_image;
            user.About = newData.About;

            TempData["message"] = "Utilizatorul a fost actualizat";
            TempData["messageType"] = "alert-success";
            db.SaveChanges();

            // Redirecționează către profilul utilizatorului
            return RedirectToAction("Show","User", new { id = id });
        }
        else
        {
            // Dacă datele trimise nu sunt valide, returnează din nou pagina cu erorile
            return View(newData);
        }
    }



    [HttpPost]
    public IActionResult Delete(string id)
    {
        var user = db.Users
                     .Where(u => u.Id == id)
                     .First();

        // Delete user comments
        
        var comments = db.Comments.Where(u => u.UserId == id); 
        foreach (var comment in comments)
        {
            var bookmars = db.Bookmarks.Where(u => u.Id == comment.BookmarkId);
            foreach(var bookmar in bookmars)
            {
                bookmar.CommentsCount -= 1;
            }
            db.Comments.Remove(comment);
        }

        //Delete user votes
        var votes = db.Votes.Where(u => u.UserId == id);
        foreach (var vote in votes)
        {
            var bookmars = db.Bookmarks.Where(u => u.Id == vote.BookmarkId);
            foreach (var bookmar in bookmars)
            {
                bookmar.VotesCount -= 1;
            }
            db.Votes.Remove(vote);
        }

        // Delete user bookmarks
        var bookmarks = db.Bookmarks.Where(u => u.UserId == id);
        foreach (var bookmark in bookmarks)
        {
            //delete bookmark comments
            var bcomments = db.Comments.Where(u => u.BookmarkId == bookmark.Id);
            foreach (var comment in bcomments)
            {
                db.Comments.Remove(comment);
            }

            //delete bookmark votes
            var bvotes = db.Votes.Where(u => u.BookmarkId == bookmark.Id);
            foreach (var vote in bvotes)
            {
                db.Votes.Remove(vote);
            }

            //delete relatia din bookmarkCategory
            var bcategories = db.CategoryBookmarks.Where(u => u.BookmarkId == bookmark.Id);
            foreach (var legatura in bcategories)
            {
                db.CategoryBookmarks.Remove(legatura);
            }
            db.Bookmarks.Remove(bookmark);
        }

        // Delete user categories
        var categories = db.Categories.Where(u => u.UserId == id);
        foreach (var category in categories)
        {
            //delete relatia din bookmarkCategory
            var bcategories = db.CategoryBookmarks.Where(u => u.CategoryId == category.Id);
            foreach(var legatura  in bcategories)
            {
                db.CategoryBookmarks.Remove(legatura);
            }
            db.Categories.Remove(category);
        }


        db.Users.Remove(user);

        db.SaveChanges();
        _signInManager.SignOutAsync(); //delogare dupa stergerea contului 

        return RedirectToAction("Index", "Bookmark");
    }


   

    // Conditiile de afisare pentru butoanele de editare si stergere
    // butoanele aflate in view-uri
    private void SetAccessRights(string userid)
    {
        ViewBag.AfisareButoane = false;
        if (User.Identity.IsAuthenticated)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == userid || User.IsInRole("Admin"))
            {
                ViewBag.AfisareButoane = true;
            }
        }

        ViewBag.UserCurent = _userManager.GetUserId(User);

        ViewBag.EsteAdmin = User.IsInRole("Admin");
    }
}
