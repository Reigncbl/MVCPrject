using Microsoft.EntityFrameworkCore;
using MVCPrject.Models;
namespace MVCPrject.Data
{
    public class RecipeContext :DbContext

    {
       
        public RecipeContext(DbContextOptions<RecipeContext> options) : base(options){}

        DbSet<Recipe> Recipes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        { 
           DotNetEnv.Env.Load();
            var server = Environment.GetEnvironmentVariable("SERVER_HOST"); 
            var port = Environment.GetEnvironmentVariable("SERVER_PORT");
            var database = Environment.GetEnvironmentVariable("DATABASE");
            var user = Environment.GetEnvironmentVariable("DATABASE_USER");
            var password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
            optionsBuilder.UseMySQL($"server={server}:{port};database={database};user={user};password={password};");
        }

    }
}
