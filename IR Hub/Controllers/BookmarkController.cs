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

        // Se afiseaza lista tuturor articolelor 
        // Pentru fiecare articol se afiseaza si userul care a postat articolul respectiv
        // [HttpGet] care se executa implicit
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

            return View();
        }

        // Se afiseaza un singur articol in functie de id-ul si toate comentariile asociate si user ul
        // [HttpGet] se executa implicit implicit
        public IActionResult Show(int id)
        {
            Bookmark bookmark = db.Bookmarks.Include("Comments")
                                         .Include("User")
                                         .Include("Comments.User")
                              .Where(bk => bk.Id == id)
                              .First();

            SetAccessRights(bookmark.UserId);

            return View(bookmark);
        }


        // adaugarea unui comentariu asociat unui articol in baza de date
        // doar cei conectati
        [HttpPost]
        //[Authorize(Roles = "User,Admin")]
        public IActionResult Show([FromForm] Comment comment)
        {
            comment.Date_created = DateTime.Now;

            comment.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Comments.Add(comment);
                db.SaveChanges();
                return Redirect("/Bookmarks/Show/" + comment.BookmarkId);
            }
            else
            {
                Bookmark bookmark = db.Bookmarks.Include("User")
                                         .Include("Comments")
                                         .Include("Comments.User")
                               .Where(bookmark => bookmark.Id == comment.BookmarkId)
                               .First();

              //  return Redirect("/Bookmark/Show/" + comment.BookmarkId);

                SetAccessRights(comment.UserId);

                return View(bookmark);
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

        
        // Doar utilizatorii cu rolul de User si Admin pot edita articole
        // Adminii pot edita orice articol din baza de date
        // Userii pot edita doar articolele proprii (cele pe care ei le-au postat)
        // [HttpGet] - se executa implicit

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
        // Se verifica rolul utilizatorilor care au dreptul sa editeze (Editor si Admin)
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


        // Utilizatorii cu rolul de User sau Admin pot sterge articole
        // Userii pot sterge doar articolele publicate de ei
        // Adminii pot sterge orice articol de baza de date

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

        [NonAction]
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
        }

    }
}
