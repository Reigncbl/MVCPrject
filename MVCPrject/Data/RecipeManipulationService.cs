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

        // Fetch a single recipe with ingredients and instructions
        public async Task<Recipe?> GetRecipeDetailsAsync(int id)
        {
            return await _dbContext.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .FirstOrDefaultAsync(r => r.RecipeID == id);
        }

        // Fetch a list of recipes (default: top 10)
        public async Task<List<Recipe>> GetAllRecipesAsync(int count = 10)
        {
            return await _dbContext.Recipes.Take(count).ToListAsync();
        }
    }
}
