using IR_Hub.Data;
using IR_Hub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IR_Hub.Controllers
{
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




        //  un singur articol in functie de id-ul si toate comentariile asociate si user ul
        // [HttpGet] se executa implicit implicit
        public IActionResult Show(int id)
        {
            Bookmark bookmark = db.Bookmarks.Include("Comments")
                                             .Include("User")
                                             .Include("Comments.User")
                                             .FirstOrDefault(bk => bk.Id == id);

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
                db.SaveChanges();
                b.CommentsCount++;//de ce nu s amodificat in  baza de date?
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
                return RedirectToAction("Show","Bookmark", new { id = comm.BookmarkId });
            }
            else
            {
                TempData["message"] = "Nu aveți dreptul să editați comentariul.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", "Bookmarks");
            }
        }


        // [HttpGet] - care se executa implicit

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

        // Se adauga articolul in baza de date
        // Doar utilizatorii cu rolul User si Admin pot adauga articole in platforma
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

        // Se adauga articolul modificat in baza de date
        // Se verifica rolul utilizatorilor care au dreptul sa editeze
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
            if (User.Identity.IsAuthenticated) {
                string currentUserId = _userManager.GetUserId(User);

                if (currentUserId == userid || User.IsInRole("Admin"))
                {
                    ViewBag.AfisareButoane = true;
                }
            }

            

            ViewBag.UserCurent = _userManager.GetUserId(User);

            ViewBag.EsteAdmin = User.IsInRole("Admin");
        }

        /*[NonAction]
        public IEnumerable<SelectListItem> GetAllCategories()
        {
            // generam o lista de tipul SelectListItem fara elemente
            var selectList = new List<SelectListItem>();

            // extragem toate categoriile din baza de date
            var categories = from cat in db.Categories
                             select cat;

            // iteram prin categorii
            foreach (var category in categories)
            {
                // adaugam in lista elementele necesare pentru dropdown
                // id-ul categoriei si denumirea acesteia
                selectList.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                });
            }

            // returnam lista de categorii
            return selectList;
        }*/

    }
}
