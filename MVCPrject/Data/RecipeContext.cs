using Microsoft.EntityFrameworkCore;
using MVCPrject.Models;
namespace MVCPrject.Data
{
    public class RecipeContext :DbContext

    {

      public   DbSet<Recipe> Recipes { get; set; }
        public RecipeContext(DbContextOptions<RecipeContext> options) : base(options){}


       


    }
}
