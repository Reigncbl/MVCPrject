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
}