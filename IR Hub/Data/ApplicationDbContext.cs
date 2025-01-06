using IR_Hub.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static IR_Hub.Models.CategoryBookmark;

namespace IR_Hub.Data
{
    public class ApplicationDbContext : IdentityDbContext<User> // pasul 3 din users si roles: am adaugat <User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
            public DbSet<User> Users { get; set; }
            public DbSet<Category> Categories { get; set; }
            public DbSet<Bookmark> Bookmarks { get; set; }
            public DbSet<Vote> Votes { get; set; }
            public DbSet<Comment> Comments { get; set; }
            public DbSet<CategoryBookmark> CategoryBookmarks { get; set; }

      
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
            

                base.OnModelCreating(modelBuilder);
            // definirea relatiei de delete dintre Comment/Vote si Bookmark/User

            /*modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments) 
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);*/

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Bookmark)
                .WithMany(b => b.Comments) 
                .HasForeignKey(c => c.BookmarkId)
                .OnDelete(DeleteBehavior.NoAction);

            /*modelBuilder.Entity<Vote>()
                .HasOne(v => v.User)
                .WithMany(u => u.Votes) 
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.NoAction);*/

            modelBuilder.Entity<Vote>()
                .HasOne(v => v.Bookmark)
                .WithMany(b => b.Votes) 
                .HasForeignKey(v => v.BookmarkId)
                .OnDelete(DeleteBehavior.NoAction);


            // definirea relatiei many-to-many dintre Categories si Bookmark
            // definire primary key compus
            modelBuilder.Entity<CategoryBookmark>()
                        .HasKey(ab => new { ab.Id, ab.BookmarkId, ab.CategoryId });

                modelBuilder.Entity<CategoryBookmark>()
                    .HasOne(ab => ab.Bookmark)
                    .WithMany(ab => ab.CategoryBookmarks)
                    .HasForeignKey(ab => ab.BookmarkId);

                modelBuilder.Entity<CategoryBookmark>()
                    .HasOne(ab => ab.Category)
                    .WithMany(ab => ab.CategoryBookmarks)
                    .HasForeignKey(ab => ab.CategoryId);
           }

    }
}
