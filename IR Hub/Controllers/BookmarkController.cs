using IR_Hub.Data;
using IR_Hub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

        // ViewBag.OriceDenumireSugestiva, am adaugat  ToList pt ca bookmarks are un query

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

        if (!string.IsNullOrEmpty(HttpContext.Request.Query["page"]) &&
            int.TryParse(HttpContext.Request.Query["page"], out int pageNumber) &&
            pageNumber > 0)
        {
            currentPage = pageNumber;
        }

        int offset = (currentPage - 1) * perPage;
        var paginatedBookmarks = bookmarks.Skip(offset).Take(perPage).ToList();

        // Setam variabilele pentru View
        ViewBag.Bookmarks = paginatedBookmarks;
        ViewBag.lastPage = Math.Ceiling((double)totalItems / perPage);
        ViewBag.PaginationBaseUrl = $"/Bookmarks/Index?sortOrder={sortOrder}&search={search}&page=";
        ViewBag.SearchString = search;


        if (TempData.ContainsKey("message"))
        {
            ViewBag.Message = TempData["message"];
            ViewBag.Alert = TempData["messageType"];
        }

        return View();
    }




    // afisarea unui singur articol in functie de id-ul si toate comentariile asociate si user ul
    // [HttpGet]  implicit

    public IActionResult Show(int id)
    {
        Bookmark bookmark = db.Bookmarks.Include("Comments")
                                     .Include("User")
                                     .Include("Comments.User")
                          .Where(bk => bk.Id == id)
                          .First();
        ViewBag.UserCategories = db.Categories
                                  .Where(b => b.UserId == _userManager.GetUserId(User))
                                  .ToList();

        if (bookmark == null)
        {
            TempData["message"] = "Marcajul specificat nu există.";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index", "Bookmark");
        }

        SetAccessRights(bookmark.UserId);
        return View(bookmark);
    }

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
            Bookmark b = db.Bookmarks.Include("User")
                                     .Include("Comments")
                                     .Include("Comments.User")
                                     .Where(b => b.Id == BookmarkId)
                                     .First();
            db.Comments.Add(comment);
            b.CommentsCount++;
            b.Comments.Add(comment);
            db.SaveChanges();
            return Redirect("/Bookmark/Show/" + BookmarkId);
        }
        else
        {
            Bookmark b = db.Bookmarks.Include("User")
                                     .Include("Comments")
                                     .Include("Comments.User")
                                     .Where(b => b.Id == BookmarkId)
                                     .First();
            //aici trebuie sa adaug si la comentariile unui user
            SetAccessRights(b.UserId);

            return View(b);
        }
    }

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

                TempData["message"] = "Articolul a fost adaugat in colectia selectata";
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

    [Authorize(Roles = "User,Admin")]
    public IActionResult DeleteComment(int BookmarkId, int id)
    {
        Comment comm = db.Comments.Include(c => c.Bookmark)
                                  .FirstOrDefault(c => c.Id == id);

        Bookmark b = db.Bookmarks.Include("User")
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
        if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
        {
            db.Comments.Remove(comm);
            b.CommentsCount--;
            b.Comments.Remove(comm);
            db.SaveChanges();
            return RedirectToAction("Show", "Bookmark", new { id = BookmarkId });
        }
        else
        {
            TempData["message"] = "Nu aveți dreptul să ștergeți comentariul.";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index", "Bookmarks");
        }
    }


    [Authorize(Roles = "User,Admin")]
    public IActionResult EditComment(int id)
    {
        Comment comm = db.Comments.Find(id);

        if (comm == null)
        {
            TempData["message"] = "Comentariul nu există.";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index", "Bookmarks", id);
        }

        if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
        {
            return View("EditComment", comm); // Specifică view-ul care editează comentariul
        }
        else
        {
            TempData["message"] = "Nu aveți dreptul să editați comentariul.";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index", "Bookmarks", id);
        }
    }

    [HttpPost]
    [Authorize(Roles = "User,Admin")]
    public IActionResult EditComment(int id, Comment updatedComment)
    {
        Comment comm = db.Comments.Find(id);

        if (comm == null)
        {
            TempData["message"] = "Comentariul nu există.";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Show", "Bookmarks");
        }

        if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
        {
            comm.Content = updatedComment.Content; // Actualizează conținutul comentariului
            db.SaveChanges();
            return RedirectToAction("Show", "Bookmark", new { id = comm.BookmarkId });
        }
        else
        {
            TempData["message"] = "Nu aveți dreptul să editați comentariul.";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Show", "Bookmarks");
        }
    }

    //adaugarea unui nou bookmark
    // [HttpGet] implicit

    [Authorize(Roles = "User,Admin")]
    public IActionResult New()
    {
        var bookmark = new Bookmark
        {//nu stiu daca sunt necesare astea de aici
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
            TempData["message"] = "Postarea a fost adaugata";
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
        Bookmark bookmark = db.Bookmarks.Find(id);

        if (ModelState.IsValid)
        {

            bookmark.Title = requestBookmark.Title;
            bookmark.Media_Content = requestBookmark.Media_Content;
            bookmark.Date_updated = DateTime.Now;
            TempData["message"] = "Poastarea a fost modificata";
            TempData["messageType"] = "alert-success";
            db.SaveChanges();
            return RedirectToAction("Index");

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
        Bookmark bookmark = db.Bookmarks.Include("Comments")
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

            //Delete user votes
            if (bookmark.VotesCount > 0)
            {
                foreach (var vote in bookmark.Votes)
                {
                    db.Votes.Remove(vote);
                }
            }
            db.Bookmarks.Remove(bookmark);
            db.SaveChanges();
            TempData["message"] = "Postarea a fost stearsa";
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