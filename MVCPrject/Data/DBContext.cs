using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // Required for IdentityDbContext
using Microsoft.EntityFrameworkCore;
using MVCPrject.Models; // Using the Models namespace for Recipe and User models

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

            modelBuilder.Entity<RecipeInstructions>()
                .HasOne(ri => ri.Recipe)
                .WithMany(r => r.Instructions)
                .HasForeignKey(ri => ri.RecipeID)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure non-nullable string properties are correctly mapped as required columns
            modelBuilder.Entity<RecipeIngredients>()
                .Property(ri => ri.IngredientName)
                .IsRequired();
            modelBuilder.Entity<RecipeIngredients>()
                .Property(ri => ri.Unit)
                .IsRequired();
            modelBuilder.Entity<RecipeInstructions>()
                .Property(ri => ri.Instruction)
                .IsRequired();
        }
    }
}
