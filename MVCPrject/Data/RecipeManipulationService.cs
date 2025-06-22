
using Microsoft.EntityFrameworkCore;
using MVCPrject.Models;

namespace MVCPrject.Data

{
    public class RecipeManipulationService
    {
        private readonly DBContext _dbContext;

        public RecipeManipulationService(DBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Recipe?> ReadRecipe(int id)
        {

            return await _dbContext.Recipes.FirstOrDefaultAsync(r => r.RecipeID == id);
              
        }

        public async Task<List<Recipe>> ReadAllRecipes()
        {
            return await _dbContext.Recipes.Take(10).ToListAsync();
        }


    }
}
