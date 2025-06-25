using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MVCPrject.Models;

namespace MVCPrject.Data
{
    public class DBContext : IdentityDbContext<User>
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options) { }

        public DbSet<Recipe> Recipes { get; set; } = null!;
        public DbSet<RecipeIngredients> RecipeIngredients { get; set; } = null!;
        public DbSet<RecipeInstructions> RecipeInstructions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RecipeIngredients>()
            .HasOne(ri => ri.Recipe)
            .WithMany(r => r.Ingredients)
            .HasForeignKey(ri => ri.RecipeID)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecipeIngredients>()
            .Property(ri => ri.Quantity)
            .HasPrecision(18, 4); // Set precision and scale for decimal

            modelBuilder.Entity<RecipeInstructions>()
            .HasOne(ri => ri.Recipe)
            .WithMany(r => r.Instructions)
            .HasForeignKey(ri => ri.RecipeID)
            .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
