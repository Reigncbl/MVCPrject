using Microsoft.AspNetCore.Mvc;
using MVCPrject.Data;
using MVCPrject.Models;
using MVCPrject.Services;

namespace MVCPrject.Controllers
{
    [Route("Recipe")]
    public class RecipeController : Controller
    {
        private readonly RecipeManipulationService _repository;
        private readonly UserService _userService;
        private readonly IUserCacheService _userCacheService;

        public RecipeController(
            RecipeManipulationService repository,
            UserService userService,
            IUserCacheService userCacheService)
        {
            _repository = repository;
            _userService = userService;
            _userCacheService = userCacheService;
        }

        [HttpGet("All")]
        public async Task<IActionResult> Recipe(string? keywords = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await SetUserViewBagAsync();
            }

            var (recipes, pageTitle) = await GetRecipesAsync(keywords);

            ViewBag.PageTitle = pageTitle;
            ViewBag.SearchKeywords = keywords;
            ViewBag.LikeCounts = await GetLikeCountsAsync(recipes);

            return View(recipes);
        }

        [HttpGet("View/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await SetUserViewBagAsync();
            }

            var recipeDetails = await _repository.GetRecipeDetailsWithNutritionAsync(id);
            return recipeDetails == null ? NotFound() : View(recipeDetails);
        }

        [HttpPost("Like")]
        public async Task<IActionResult> LikeRecipe([FromBody] LikeRequest request)
        {
            return await HandleLikeAction(request, isLike: true);
        }

        [HttpPost("Unlike")]
        public async Task<IActionResult> UnlikeRecipe([FromBody] LikeRequest request)
        {
            return await HandleLikeAction(request, isLike: false);
        }

        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] AddRecipeRequest request)
        {
            try
            {
                // Get the current authenticated user as the recipe author
                string recipeAuthor = "Anonymous";
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userInfo = await _userCacheService.GetUserInfoAsync(User);
                    if (userInfo != null)
                    {
                        recipeAuthor = userInfo.DisplayName ?? userInfo.Email ?? "Anonymous";
                    }
                }

                // Create recipe object from request
                var recipe = new Recipe
                {
                    RecipeName = request.RecipeName,
                    RecipeDescription = request.Description,
                    RecipeAuthor = recipeAuthor,
                    RecipeServings = request.Servings?.ToString(),
                    CookTimeMin = request.CookingTime,
                    PrepTimeMin = 0, // Not captured in modal, set to 0
                    RecipeType = "Main Course", // Default type, could be enhanced later
                    RecipeImage = request.ImageUrl,
                    RecipeURL = null
                };

                var recipeId = await _repository.AddRecipeAsync(recipe);

                // Add ingredients if provided
                if (request.Ingredients?.Any() == true)
                {
                    await _repository.AddRecipeIngredientsAsync(recipeId, request.Ingredients);
                }

                // Add instructions if provided
                if (request.Instructions?.Any() == true)
                {
                    await _repository.AddRecipeInstructionsAsync(recipeId, request.Instructions);
                }

                // Add nutrition facts if provided
                if (request.Calories.HasValue || request.Protein.HasValue || request.Carbs.HasValue || request.Fat.HasValue)
                {
                    await _repository.AddRecipeNutritionAsync(recipeId, request.Calories, request.Protein, request.Carbs, request.Fat);
                }

                return Json(new { success = true, recipeId = recipeId, message = "Recipe added successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while adding the recipe. Please try again." });
            }
        }

        private async Task SetUserViewBagAsync()
        {
            var userInfo = await _userCacheService.GetUserInfoAsync(User);
            if (userInfo != null)
            {
                ViewBag.UserName = userInfo.DisplayName;
                ViewBag.UserEmail = userInfo.Email;
            }
        }

        private async Task<(List<Recipe> recipes, string pageTitle)> GetRecipesAsync(string? keywords)
        {
            return string.IsNullOrEmpty(keywords)
                ? (await _repository.GetAllRecipesAsync(), "All Recipes")
                : (await _repository.SearchRecipesByIngredientsAsync(keywords), "Search Results");
        }

        private async Task<Dictionary<int, int>> GetLikeCountsAsync(List<Recipe> recipes)
        {
            var likeCounts = new Dictionary<int, int>();
            foreach (var recipe in recipes)
            {
                likeCounts[recipe.RecipeID] = await _userService.GetLikeCountAsync(recipe.RecipeID);
            }
            return likeCounts;
        }

        private async Task<IActionResult> HandleLikeAction(LikeRequest request, bool isLike)
        {
            var user = await _userCacheService.GetCurrentUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false });
            }

            var result = isLike
                ? await _userService.AddLike(request.RecipeId, user.Id)
                : await _userService.RemoveLike(request.RecipeId, user.Id);

            if (result == 1)
            {
                var likeCount = await _userService.GetLikeCountAsync(request.RecipeId);
                return Json(new { success = true, likeCount });
            }

            return Json(new { success = false });
        }
    }

    public class AddRecipeRequest
    {
        public string? RecipeName { get; set; }
        public string? Description { get; set; }
        public string? RecipeAuthor { get; set; }
        public int? Servings { get; set; }
        public int? CookingTime { get; set; }
        public int? Calories { get; set; }
        public int? Protein { get; set; }
        public int? Carbs { get; set; }
        public int? Fat { get; set; }
        public List<string>? Ingredients { get; set; }
        public List<string>? Instructions { get; set; }
        public string? ImageUrl { get; set; }
    }
}