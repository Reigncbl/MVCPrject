using Microsoft.EntityFrameworkCore;
using MVCPrject.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace MVCPrject.Data
{
    public class DBContext : IdentityDbContext<User> //
    {
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeIngredients> RecipeIngredients { get; set; }
        public DbSet<RecipeInstructions> RecipeInstructions { get; set; }
        public DBContext(DbContextOptions<DBContext> options) : base(options) { }
    }
}
