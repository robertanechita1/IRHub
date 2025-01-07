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

    private readonly RoleManager<IdentityRole> _roleManager;

    public UserController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager
        )
    {
        db = context;

        _userManager = userManager;

        _roleManager = roleManager;
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
        var user = db.Users.FirstOrDefault(u => u.Id == userId);

        if (user == null)
        {
            return NotFound();
        }

        SetAccessRights(id);

        // un URL implicit pentru poza de profil, daca nu exista
        user.Profile_image = string.IsNullOrEmpty(user.Profile_image)
            ? "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ9eLeSj523CsBb4S2TNM4BZ-8TuObk0YsoaFQVvATuYEGEXLqxjIqAxOJh0z2xgU1kPzc&usqp=CAU"
            : user.Profile_image;

        var UserCategories = db.Categories.Where(u => u.UserId == userId);
        ViewBag.UserCategories = UserCategories;

        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfileImage(string profileImageUrl)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!string.IsNullOrEmpty(profileImageUrl))
        {
            currentUser.Profile_image = profileImageUrl;
            await _userManager.UpdateAsync(currentUser);
        }

        return RedirectToAction(nameof(Show), new { id = currentUser.Id });
    }




    public async Task<ActionResult> Edit(string id)
    {
        User user = db.Users.Find(id);

        ViewBag.AllRoles = GetAllRoles();

        var roleNames = await _userManager.GetRolesAsync(user); // Lista de nume de roluri

        // Cautam ID-ul rolului in baza de date
        ViewBag.UserRole = _roleManager.Roles
                                          .Where(r => roleNames.Contains(r.Name))
                                          .Select(r => r.Id)
                                          .First(); // Selectam 1 singur rol

        return View(user);
    }

    [HttpPost]
    public async Task<ActionResult> Edit(string id, User newData, [FromForm] string newRole)
    {
        User user = db.Users.Find(id);

       // user.AllRoles = GetAllRoles();


        if (ModelState.IsValid)
        {
            user.UserName = newData.UserName;
            user.Email = newData.Email;
            user.FirstName = newData.FirstName;
            user.LastName = newData.LastName;
            user.PhoneNumber = newData.PhoneNumber;


            // Cautam toate rolurile din baza de date
            var roles = db.Roles.ToList();

            foreach (var role in roles)
            {
                // Scoatem userul din rolurile anterioare
                await _userManager.RemoveFromRoleAsync(user, role.Name);
            }
            // Adaugam noul rol selectat
            var roleName = await _roleManager.FindByIdAsync(newRole);
            await _userManager.AddToRoleAsync(user, roleName.ToString());

            db.SaveChanges();

        }
        return RedirectToAction("Index");
    }


    [HttpPost]
    public IActionResult Delete(string id)
    {
        var user = db.Users
                     .Include("Articles")
                     .Include("Comments")
                     .Include("Bookmarks")
                     .Where(u => u.Id == id)
                     .First();

        // Delete user comments
        
        var comments = db.Comments.Where(u => u.UserId == id); 
        foreach (var comment in comments)
        {
              db.Comments.Remove(comment);
        }

        //Delete user votes
        var votes = db.Votes.Where(u => u.UserId == id);
        foreach (var vote in votes)
        {
            db.Votes.Remove(vote);
        }

        // Delete user bookmarks
        var bookmarks = db.Bookmarks.Where(u => u.UserId == id);
        foreach (var bookmark in bookmarks)
        {
            db.Bookmarks.Remove(bookmark);
        }

        // Delete user categories
        var categories = db.Categories.Where(u => u.UserId == id);
        foreach (var category in categories)
        {
            db.Categories.Remove(category);
        }

        db.Users.Remove(user);

        db.SaveChanges();

        return RedirectToAction("Index");
    }


    [NonAction]
    public IEnumerable<SelectListItem> GetAllRoles()
    {
        var selectList = new List<SelectListItem>();

        var roles = from role in db.Roles
                    select role;

        foreach (var role in roles)
        {
            selectList.Add(new SelectListItem
            {
                Value = role.Id.ToString(),
                Text = role.Name.ToString()
            });
        }
        return selectList;
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
