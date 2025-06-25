using System;
using Microsoft.EntityFrameworkCore;
using MVCPrject.Models;
using LinqKit;

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
                .AsSplitQuery()
                .FirstOrDefaultAsync(r => r.RecipeID == id);
        }


        public async Task<List<Recipe>> GetAllRecipesAsync(int count = 10)
        {
            return await _dbContext.Recipes.OrderBy(r => r.RecipeID).Take(count).ToListAsync();
        }


        public async Task<List<Recipe>> SearchRecipesByIngredientsAsync(string keywords)
        {
            var keywordList = keywords.Split(',')
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrEmpty(k))
                .ToArray();

            var query = _dbContext.Recipes.AsQueryable();

            if (keywordList.Any())
            {
                var predicate = PredicateBuilder.New<Recipe>();
                foreach (var keyword in keywordList)
                {
                    var k = keyword; // Local variable for closure
                    predicate = predicate.Or(r =>
                        r.Ingredients.Any(i => EF.Functions.Like(i.IngredientName, $"%{k}%")) ||
                        EF.Functions.Like(r.RecipeName, $"%{k}%") ||
                        EF.Functions.Like(r.RecipeAuthor, $"%{k}%") ||
                        EF.Functions.Like(r.RecipeType, $"%{k}%"));
                }
                query = query.Where(predicate);
            }

            return await query
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .OrderBy(r => r.RecipeID)
                .AsSplitQuery()
                .Take(300).ToListAsync();
        }

        private bool IsRecipeTypeKeyword(string keyword)
        {
            var recipeTypes = new[] { "Dinner", "Breakfast", "Lunch", "Snack", "Dessert",
                              "Main Course", "Appetizer", "Side Dish", "Soup", "Salad" };
            return recipeTypes.Contains(keyword, StringComparer.OrdinalIgnoreCase);
        }


    }
}
