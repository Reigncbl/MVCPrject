using Microsoft.AspNetCore.Mvc;
using MVCPrject.Data;

namespace MVCPrject
{
    [Route("Recipe")]
    public class RecipeController : Controller
    {
        private readonly RecipeManipulationService _repository;

        public RecipeController(RecipeManipulationService repository)
        {
            _repository = repository;
        }

        // Action to fetch all recipes
        [HttpGet("All")]
        public async Task<IActionResult> Recipe()
        {
            var recipes = await _repository.GetAllRecipesAsync();
            return View(recipes);
        }

        // Action to fetch a single recipe's details
        [HttpGet("View/{id:int}")] // Changed from "Details" to "View" to avoid conflict
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _repository.GetRecipeDetailsAsync(id);
            if (recipe == null)
            {
                return NotFound();
            }
            return View(recipe);
        }
    }
}
