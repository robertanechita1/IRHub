﻿using IR_Hub.Data;
using IR_Hub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Imaging;
using System.Drawing;
using System.Net;
using System.Linq;

namespace IR_Hub.Controllers;

public class BookmarkController : Controller
{
    private readonly ApplicationDbContext db;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    public BookmarkController(
    ApplicationDbContext context,
    UserManager<User> userManager,
    RoleManager<IdentityRole> roleManager
    )
    {
        db = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }


    // [HttpGet] se executa implicit
    //afisarea tuturor bookmarks urilor
    public IActionResult Index(string sortOrder = "popular")
    {
        var bookmarks = db.Bookmarks.Include("User");

        // in functie de ce se afla in stringul primit ca parametru va sorta bookmark-urile
        switch (sortOrder)
        {
            case "recent":
                bookmarks = bookmarks
                    .OrderByDescending(b => b.Date_created)
                    .ThenByDescending(b => b.VotesCount);
                break;
            case "popular":
            default:
                bookmarks = bookmarks
                    .OrderByDescending(b => b.VotesCount)
                    .ThenByDescending(b => b.Date_created);
                break;
        }

        //am adaugat  ToList pt ca bookmarks are un query

        ViewBag.Bookmarks = bookmarks.ToList();

        if (TempData.ContainsKey("message"))
        {
            ViewBag.Message = TempData["message"];
            ViewBag.Alert = TempData["messageType"];
        }

        //MOTOR DE CAUTARE

        var search = HttpContext.Request.Query["search"].ToString().Trim();
        if (!string.IsNullOrEmpty(search))
        {
            // Cautare in bookmarks
            var bookmarkId = db.Bookmarks
                                .Where(b => b.Title.Contains(search) ||
                                            b.Description.Contains(search) ||
                                            b.Media_Content.Contains(search))
                                .Select(b => b.Id)
                                .ToList();

            // Cautare in comentarii
            var bookmarkIds_Comments = db.Comments
                                                          .Where(c => c.Content.Contains(search))
                                                          .Select(c => (int)c.BookmarkId)
                                                          .ToList();

            // Unim ID-urile relevante
            var mergedIds = bookmarkId.Union(bookmarkIds_Comments).ToList();
            bookmarks = bookmarks.Where(b => mergedIds.Contains(b.Id));
        }

        int perPage = 9;
        int totalItems = bookmarks.Count();
        int currentPage = 1;

        // Obține pagina curentă din QueryString
        if (!string.IsNullOrEmpty(HttpContext.Request.Query["page"]) &&
            int.TryParse(HttpContext.Request.Query["page"], out int pageNumber) &&
            pageNumber > 0)
        {
            currentPage = pageNumber;
        }


        int lastPage = (int)Math.Ceiling((double)totalItems / perPage);

        // Asigură-te că pagina curentă este validă
        if (currentPage > lastPage) currentPage = lastPage;
        if (totalItems == 0) lastPage = currentPage = 1;

        // Calculează offset-ul și extrage bookmark-urile paginate
        int offset = (currentPage - 1) * perPage;
        var paginatedBookmarks = bookmarks.Skip(offset).Take(perPage).ToList();

        // Setează variabilele pentru View
        ViewBag.Bookmarks = paginatedBookmarks;
        ViewBag.lastPage = lastPage;
        ViewBag.currentPage = currentPage;
        ViewBag.PaginationBaseUrl = $"/Bookmark/Index?sortOrder={sortOrder}&search={search}&page=";

        ViewBag.SearchString = search;

        return View();


    }


    // afisarea unui singur articol in functie de id-ul si toate comentariile asociate si user ul
    public IActionResult Show(int id)
    {
        Bookmark bookmark = db.Bookmarks.Include("Comments")
                                     .Include("User")
                                     .Include("Comments.User")
                          .Where(bk => bk.Id == id)
                          .First();
        ViewBag.UserCategories = db.Categories //pentru categoriile din butonul Save
                                  .Where(b => b.UserId == _userManager.GetUserId(User))
                                  .ToList();

        if (bookmark == null)
        {
            TempData["message"] = "Marcajul specificat nu există.";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index", "Bookmark");
        }

        SetAccessRights(bookmark.UserId);
        if (TempData.ContainsKey("message"))
        {
            ViewBag.Message = TempData["message"];
            ViewBag.Alert = TempData["messageType"];
        }
        return View(bookmark);
    }

    //adaugare comentariu
    [HttpPost]
    [Authorize(Roles = "User,Admin")]
    public IActionResult NewComment(int BookmarkId, string Cont)
    {
        var comment = new Comment
        {
            BookmarkId = BookmarkId,
            Content = Cont,
            Date_created = DateTime.Now,
            UserId = _userManager.GetUserId(User)
        };

        if (ModelState.IsValid)
        {
            db.Comments.Add(comment);
            var b = db.Bookmarks.Include("Comments").FirstOrDefault(b => b.Id == BookmarkId);
            b.CommentsCount++;
            db.SaveChanges();

            TempData["message"] = "Comentariul a fost adăugat cu succes!";
            TempData["messageType"] = "alert-success";
        }
        else
        {
            TempData["message"] = "Eroare la adăugarea comentariului.";
            TempData["messageType"] = "alert-danger";
        }

        return RedirectToAction("Show", new { id = BookmarkId });
    }


    //adaugarea unui bookmark intr-o categorie proprie
    [HttpPost]
    [Authorize(Roles = "User,Admin")]
    public IActionResult AddCategory([FromForm] CategoryBookmark categBookmark)
    {
        if (ModelState.IsValid)
        {
            if (db.CategoryBookmarks
                .Where(ab => ab.BookmarkId == categBookmark.BookmarkId)
                .Where(ab => ab.CategoryId == categBookmark.CategoryId)
                .Count() > 0)
            {
                TempData["message"] = "Acest articol este deja adaugat in colectie";
                TempData["messageType"] = "alert-danger";
            }
            else
            {
                db.CategoryBookmarks.Add(categBookmark);
                db.SaveChanges();

                TempData["message"] = "Articolul a fost adăugat în colecția selectată!";
                TempData["messageType"] = "alert-success";
            }
        }
        else
        {
            TempData["message"] = "Nu s-a putut adauga articolul in colectie";
            TempData["messageType"] = "alert-danger";
        }

        return Redirect("/Bookmark/Show/" + categBookmark.BookmarkId);
    }

    //stergerea unui comentariu propriu sau de catre admin
    [Authorize(Roles = "User,Admin")]
    public IActionResult DeleteComment(int BookmarkId, int id)
    {
        var comm = db.Comments.Include(c => c.Bookmark)
                                  .FirstOrDefault(c => c.Id == id);

        var b = db.Bookmarks.Include("User")
                     .Include("Comments")
                     .Include("Comments.User")
                     .FirstOrDefault(b => b.Id == BookmarkId); // Folosește doar FirstOrDefault
        if (b == null)
        {
            TempData["message"] = "Marcajul specificat nu există.";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index", "Bookmark");
        }

        // Verifică drepturile utilizatorului
        if (comm!.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
        {
            db.Comments.Remove(comm);
            b.CommentsCount--;
            b.Comments!.Remove(comm); //! pentru a nu mai returna error
            db.SaveChanges();

            TempData["message"] = "Comentariul a fost sters cu succes!";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Show", "Bookmark", new { id = BookmarkId });
        }
        else
        {
            TempData["message"] = "Nu aveți dreptul să ștergeți comentariul.";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index", "Bookmark");
        }
    }

    //editare comentariu
    [Authorize(Roles = "User,Admin")]
    public IActionResult EditComment(int id)
    {
        var comm = db.Comments.Find(id);

        if (comm == null)
        {
            TempData["message"] = "Comentariul nu există.";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index", "Bookmark", id);
        }

        if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
        {
            return View("EditComment", comm); // Specifică view-ul care editează comentariul
        }
        else
        {
            TempData["message"] = "Nu aveți dreptul să editați comentariul.";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index", "Bookmark", id);
        }
    }

    [HttpPost]
    [Authorize(Roles = "User,Admin")]
    public IActionResult EditComment(int id, Comment updatedComment)
    {
        var comm = db.Comments.Find(id);

        if (comm == null)
        {
            TempData["message"] = "Comentariul nu există.";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Show", "Bookmark", new { id = comm.BookmarkId });
        }

        if(string.IsNullOrEmpty(updatedComment.Content))
        {
            TempData["message"] = "Conținutul comentariului nu poate fi gol.";
            TempData["messageType"] = "alert-danger";
            return View(comm);
        }

        else
        {
            if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                comm.Content = updatedComment.Content; // Actualizează conținutul comentariului
                db.SaveChanges();
                TempData["message"] = "Comentariul a fost modificat!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Show", "Bookmark", new { id = comm.BookmarkId });
            }
            else
            {
                TempData["message"] = "Nu aveți dreptul să editați comentariul.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", "Bookmark", new { id = comm.BookmarkId });
            }
        }
       
    }

    //adaugarea unui nou bookmark

    [Authorize(Roles = "User,Admin")]
    public IActionResult New()
    {
        var bookmark = new Bookmark
        {
            Date_created = DateTime.Now,
            UserId = _userManager.GetUserId(User),
            User = db.Users.FirstOrDefault(u => u.Id == _userManager.GetUserId(User)),
            VotesCount = 0,
            CommentsCount = 0,
            Votes = new List<Vote>(),
            Comments = new List<Comment>()
        };

        return View(bookmark);
    }


    //adaugarea dupa ce am primit info din forms
    [HttpPost]
    [Authorize(Roles = "User,Admin")]
    public IActionResult New(Bookmark bookmark)
    {
        if (ModelState.IsValid)
        {
            bookmark.UserId = _userManager.GetUserId(User);
            bookmark.User = db.Users.FirstOrDefault(u => u.Id == _userManager.GetUserId(User));
            bookmark.VotesCount = 0;
            bookmark.CommentsCount = 0;
            db.Bookmarks.Add(bookmark);
            db.SaveChanges();
            TempData["message"] = "Postarea a fost adăugată cu succes!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index", "Bookmark");
        }
        else
        {
            return View(bookmark);
        }
    }

    //editarea unui bookmark

    [Authorize(Roles = "User,Admin")]
    public IActionResult Edit(int id)
    {

        Bookmark bookmark = db.Bookmarks
                  .Where(bkm => bkm.Id == id)
                  .First();


        if ((bookmark.UserId == _userManager.GetUserId(User)) ||
            User.IsInRole("Admin"))
        {
            return View(bookmark);
        }
        else
        {

            TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui articol care nu va apartine";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index");
        }
    }

    //editarea dupa ce am primit info din forms
    [HttpPost]
    [Authorize(Roles = "User,Admin")]
    public IActionResult Edit(int id, Bookmark requestBookmark)
    {
        var bookmark = db.Bookmarks.Find(id);

        if (ModelState.IsValid)
        {

            bookmark.Title = requestBookmark.Title;
            bookmark.Media_Content = requestBookmark.Media_Content;
            bookmark.Date_updated = DateTime.Now;
            TempData["message"] = "Postarea a fost modificată cu succes!";
            TempData["messageType"] = "alert-success";
            db.SaveChanges();
            return RedirectToAction("Show", new { id = bookmark.Id });

        }
        else
        {
            return View(requestBookmark);
        }
    }



    //stergerea unui bookmark
    [HttpPost]
    [Authorize(Roles = "User,Admin")]
    public ActionResult Delete(int id)
    {
        var bookmark = db.Bookmarks.Include("Comments")
                                    .Include("Votes")
                                     .Where(art => art.Id == id)
                                     .First();

        if ((bookmark.UserId == _userManager.GetUserId(User))
                || User.IsInRole("Admin"))
        {
            // Delete bookmark comments
            if (bookmark.CommentsCount > 0)
            {
                foreach (var comment in bookmark.Comments)
                {
                    db.Comments.Remove(comment);
                }
            }

            //Delete bookmark votes
            if (bookmark.VotesCount > 0)
            {
                foreach (var vote in bookmark.Votes)
                {
                    db.Votes.Remove(vote);
                }
            }
            db.Bookmarks.Remove(bookmark);
            db.SaveChanges();
            TempData["message"] = "Postarea a fost stearsă!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index");
        }
        else
        {
            TempData["message"] = "Nu aveti dreptul sa stergeti o postare care nu va apartine";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index");
        }
    }

    // Conditiile de afisare pentru butoanele de editare si stergere
    // butoanele aflate in view-uri
    private void SetAccessRights(string userid)
    {
        ViewBag.AfisareButoane = false;
        if (User.Identity.IsAuthenticated)
        {
            string currentUserId = _userManager.GetUserId(User);

            if (currentUserId == userid || User.IsInRole("Admin"))
            {
                ViewBag.AfisareButoane = true;
            }
        }



        ViewBag.UserCurent = _userManager.GetUserId(User);

        ViewBag.EsteAdmin = User.IsInRole("Admin");
    }

    
}