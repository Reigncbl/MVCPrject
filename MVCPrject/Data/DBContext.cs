using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MVCPrject.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace MVCPrject.Data
{
    public class DBContext : DbContext
    {
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeIngredients> RecipeIngredients { get; set; }
        public DbSet<RecipeInstructions> RecipeInstructions { get; set; }

        public DBContext(DbContextOptions<DBContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RecipeIngredients>()
                .Property(r => r.Quantity)
                .HasPrecision(10, 4);
        }
    }

}
