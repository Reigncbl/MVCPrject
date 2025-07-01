using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MVCPrject.Data;
using MVCPrject.Models;

namespace MVCPrject.Services
{
    public class SuggestionService
    {
        private readonly DBContext _context;
        public SuggestionService(DBContext context)
        {
            _context = context;
        }

        public async Task<List<Suggestion>> GetUserSuggestionsAsync(string userId)
        {
            return await _context.RecipeLikes
                .Where(rl => rl.UserID == userId)
                .Join(_context.Recipes.Include(r => r.Author),
                      like => like.RecipeID,
                      recipe => recipe.RecipeID,
                      (like, recipe) => new Suggestion
                      {
                          recipeLikes = like,
                          recipe = recipe
                      })
                .Take(4).ToListAsync();
        }
    }
}
