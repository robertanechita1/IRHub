using IR_Hub.Data;
using IR_Hub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IR_Hub.Controllers;

//[Authorize(Roles = "User,Admin")]
public class CategoryController : Controller
{

    private readonly ApplicationDbContext db;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    public CategoryController(
    ApplicationDbContext context,
    UserManager<User> userManager,
    RoleManager<IdentityRole> roleManager
    )
    {
        db = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }
    public ActionResult Index()
    {
        if (TempData.ContainsKey("message"))
        {
            ViewBag.message = TempData["message"].ToString();
        }

        var categories = from category in db.Categories
                         orderby category.Name
                         select category;
        ViewBag.Categories = categories;
        return View();
    }

    public ActionResult Show(int id)
    {
        var category = db.Categories.Find(id);
        var bookmarkCategs = db.CategoryBookmarks.Where(u => u.CategoryId == id);

        // extragem toate BookmarkId-urile intr-o
        var bookmarkIds = bookmarkCategs.Select(rel => rel.BookmarkId).ToList();

        var bookmarks = db.Bookmarks.Where(b => bookmarkIds.Contains(b.Id)).ToList();

        ViewBag.Bookmarks = bookmarks;
        SetAccessRights(category.UserId);

        return View(category);

    }

    // [HttpGet] implicit

    [Authorize(Roles = "User,Admin")]
    public IActionResult New()
    {
        var categ = new Category
        {
            Date_created = DateTime.Now,
            UserId = _userManager.GetUserId(User),
        };

        return View(categ);
    }


    //adaugarea dupa ce am primit info din forms
    [HttpPost]
    [Authorize(Roles = "User,Admin")]
    public IActionResult New(Category categ)
    {
        if (ModelState.IsValid)
        {
            // Obține utilizatorul curent
            var userId = _userManager.GetUserId(User);
            var user = db.Users.FirstOrDefault(u => u.Id == userId);

            categ.UserId = userId;

            categ.Date_created = DateTime.Now;
            categ.Date_updated = DateTime.Now;

            // Setează vizibilitatea în funcție de starea checkbox-ului
            categ.visibility = categ.visibility; // Va fi true dacă checkbox-ul este selectat, false dacă nu este selectat

            categ.Description = "Fără descriere.";


            db.Categories.Add(categ);

            db.SaveChanges();

            // Mesaj de succes
            TempData["message"] = "Categoria a fost adăugată!";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Show","User", new { id = categ.UserId }); //redirectionare inapoi in profil
        }
        else
        {
            // Dacă validarea eșuează, redirecționează înapoi la formularul de creare
            return View(categ);
        }
    }



    public ActionResult Edit(int id)
    {
        Category category = db.Categories.Find(id);
        return View(category);
    }

    [HttpPost]
    public ActionResult Edit(int id, Category requestCategory)
    {
        Category category = db.Categories.Find(id);

        if (ModelState.IsValid)
        {

            category.Name = requestCategory.Name;
            db.SaveChanges();
            TempData["message"] = "Categoria a fost modificata!";
            return RedirectToAction("Index");
        }
        else
        {
            return View(requestCategory);
        }
    }

    [HttpPost]
    public ActionResult Delete(int id)
    {

        Category category = db.Categories.Where(c => c.Id == id)
                                         .First();
        var userrId = category.UserId;
        var bookmarkCategories = db.CategoryBookmarks.Where(c => c.CategoryId == id);
        foreach(var bookmarkcateg in bookmarkCategories)
        {
            db.CategoryBookmarks.Remove(bookmarkcateg);
        }
        db.Categories.Remove(category);

        TempData["message"] = "Categoria a fost stearsa";
        db.SaveChanges();
        return RedirectToAction("Show", "User", new { id = userrId });
    }

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
    }
}
