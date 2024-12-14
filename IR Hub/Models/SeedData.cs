using IR_Hub.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace IR_Hub.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Verificam daca in baza de date exista cel putin un rol
                // insemnand ca a fost rulat codul
                // De aceea facem return pentru a nu insera rolurile inca o data
                // Acesta metoda trebuie sa se execute o singura data
                if (context.Roles.Any())
                {
                    return; // baza de date contine deja roluri
                }
                
                // daca nu contine roluri, acestea se vor crea
                context.Roles.AddRange(

                new IdentityRole
                {
                    Id = "2c5e174e-3b0e-446f-86af-483d56fd7210",
                    Name = "Admin",
                    NormalizedName = "Admin".ToUpper()
                },


                new IdentityRole
                {
                    Id = "2c5e174e-3b0e-446f-86af-483d56fd7212",
                    Name = "User",
                    NormalizedName = "User".ToUpper()
                }


                );

                // o noua instanta pe care o vom utiliza pentru crearea parolelor utilizatorilor
                // parolele sunt de tip hash
                var hasher = new PasswordHasher<User>();

                // CREAREA USERILOR IN BD
                // Se creeaza cate un user pentru fiecare rol
                context.Users.AddRange(//to check with the database
                new User
                {
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb0",
                    UserName = "admin@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "ADMIN@TEST.COM",
                    Email = "admin@test.com",
                    PhoneNumberConfirmed = true,
                    TwoFactorEnabled = false,
                    LockoutEnabled = false,
                    AccessFailedCount = 0,
                    NormalizedUserName = "ADMIN@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "Admin1!"),
                    FirstName = "admin",
                    LastName = "admin",
                    About = "Acesta este contul adminului",
                    Profile_image = "/images/default.png",
                    Role = "admin"
                },
                
                new User
                {
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb2",
                    UserName = "user@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "USER@TEST.COM",
                    Email = "user@test.com",
                    PhoneNumberConfirmed = true,
                    TwoFactorEnabled = false,
                    LockoutEnabled = false,
                    AccessFailedCount = 0,
                    NormalizedUserName = "USER@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "User1!"),
                    FirstName = "user",
                    LastName = "user",
                    About = "Acesta este contul userului",
                    Profile_image = "/images/default.png",
                    Role = "user"
                }
    );

                // ASOCIEREA USER-ROLE
                context.UserRoles.AddRange(
                new IdentityUserRole<string>
                {

                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7210",


                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb0"
                },

                new IdentityUserRole<string>

                {

                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7212",


                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb2"
                }
                );

                context.SaveChanges();
            }
        }
    }
}
