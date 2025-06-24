using Microsoft.AspNetCore.Mvc;
using MVCPrject.Data;
using MVCPrject.Models;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

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
        public async Task<IActionResult> Recipe(string keywords = null)
        {
            List<Recipe> recipes;

            if (string.IsNullOrEmpty(keywords))
            {
                recipes = await _repository.GetAllRecipesAsync();
                ViewBag.PageTitle = "All Recipes";
            }
            else
            {
                recipes = await _repository.SearchRecipesByIngredientsAsync(keywords);
                ViewBag.PageTitle = "Search Results";
                ViewBag.SearchKeywords = keywords;
            }

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

        [HttpGet("Search")]
        public async Task<IActionResult> Search(string keywords)
        {
            return RedirectToAction("Recipe", new { keywords = keywords });
        }

    }
}
