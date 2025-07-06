using Microsoft.AspNetCore.Mvc;
using MVCPrject.Data;
using MVCPrject.Models;
using MVCPrject.Services;
using Azure.Storage.Blobs;
using System.Security.Claims;
namespace MVCPrject.Controllers
{
    [Route("Recipe")]
    public class RecipeController : Controller
    {
        private readonly RecipeManipulationService _repository;
        private readonly UserService _userService;
        private readonly IUserCacheService _userCacheService;

        private readonly BlobServiceClient _blobServiceClient;
        private const int PageSize = 20;

        public RecipeController(
       RecipeManipulationService repository,
       UserService userService,
       IUserCacheService userCacheService,
       BlobServiceClient blobServiceClient)
        {
            _repository = repository;
            _userService = userService;
            _userCacheService = userCacheService;
            _blobServiceClient = blobServiceClient;
        }

        [HttpGet("Recipe")]
        public async Task<IActionResult> RecipeAction(string? keywords = null, int pageNumber = 1, string? source = null)
        {
            Console.WriteLine($"[DEBUG] RecipeAction called with keywords: '{keywords}', pageNumber: {pageNumber}, source: '{source}'");
            
            if (User.Identity?.IsAuthenticated == true)
            {
                await SetUserViewBagAsync();
            }

            if (pageNumber < 1) pageNumber = 1;

            var (allRecipes, pageTitle) = await GetRecipesAsync(keywords, source);

            Console.WriteLine($"[DEBUG] GetRecipesAsync returned {allRecipes.Count} recipes with title: '{pageTitle}'");

            var paginatedRecipes = PaginatedList<Recipe>.Create(allRecipes, pageNumber, PageSize);

            Console.WriteLine($"[DEBUG] Paginated to {paginatedRecipes.Count} recipes on page {pageNumber}");

            ViewBag.PageTitle = pageTitle;
            ViewBag.SearchKeywords = keywords;
            ViewBag.CurrentKeywords = keywords;
            ViewBag.Source = source;
            ViewBag.LikeCounts = await GetLikeCountsAsync(paginatedRecipes.ToList());

            return View("Recipe", paginatedRecipes);
        }
        [HttpGet("View/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await SetUserViewBagAsync();
            }

            var recipeDetails = await _repository.GetRecipeDetailsWithNutritionAsync(id);
            if (recipeDetails == null) 
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.CurrentUserId = currentUserId;

            // This line sets the flag for highlighting "Profile"
            ViewBag.IsOwnRecipe = recipeDetails.Recipe?.AuthorId == currentUserId;

            // Get the like count for this recipe
            ViewBag.LikeCount = await _userService.GetLikeCountAsync(id);
            ViewBag.CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return View(recipeDetails);
        }

        [HttpPost("DeleteRecipe/{id}")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            var currentUser = await _userCacheService.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var recipe = await _repository.GetRecipeByIdAsync(id);
            if (recipe == null || recipe.AuthorId != currentUser.Id)
                return Forbid();

            var result = await _userService.DeleteRecipeAsync(id);
            if (result)
                return RedirectToAction("Profile", "Profile");

            return BadRequest("Failed to delete recipe.");
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

        [HttpGet("Search")]
        public async Task<IActionResult> SearchRecipes(string query = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Json(new { success = true, recipes = new List<object>() });
                }

                var recipes = await _repository.SearchRecipesByIngredientsAsync(query);

                var recipeResults = new List<object>();

                foreach (var recipe in recipes.Take(10)) // Limit to 10 results for performance
                {
                    // Get nutrition facts for each recipe
                    var nutritionFacts = await GetRecipeNutritionAsync(recipe.RecipeID);

                    recipeResults.Add(new
                    {
                        id = recipe.RecipeID,
                        name = recipe.RecipeName,
                        description = recipe.RecipeDescription,
                        author = recipe.Author?.Name ?? "Unknown Author",
                        type = recipe.RecipeType,
                        servings = recipe.RecipeServings,
                        cookTime = recipe.CookTimeMin,
                        prepTime = recipe.PrepTimeMin,
                        totalTime = recipe.TotalTimeMin,
                        image = recipe.RecipeImage,
                        calories = nutritionFacts?.Calories,
                        protein = nutritionFacts?.Protein,
                        carbs = nutritionFacts?.Carbohydrates,
                        fat = nutritionFacts?.Fat
                    });
                }

                return Json(new { success = true, recipes = recipeResults });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error searching recipes" });
            }
        }

        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] AddRecipeRequest request)
        {
            try
            {
                string? authorId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userInfo = await _userCacheService.GetCurrentUserAsync(User);
                    authorId = userInfo?.Id;
                }

                // Allow recipes without authors for backward compatibility
                // if (string.IsNullOrEmpty(authorId))
                // {
                //     return Json(new { success = false, message = "User must be logged in to add recipes." });
                // }

                string? uploadedImageUrl = null;

                // Upload image to Azure Blob Storage if provided
                if (!string.IsNullOrEmpty(request.ImageUrl))
                {
                    var containerClient = _blobServiceClient.GetBlobContainerClient("recipes");
                    await containerClient.CreateIfNotExistsAsync();

                    var blobName = $"{Guid.NewGuid()}-{Path.GetFileName(request.ImageUrl)}";
                    var blobClient = containerClient.GetBlobClient(blobName);

                    using (var stream = new MemoryStream(Convert.FromBase64String(request.ImageUrl)))
                    {
                        await blobClient.UploadAsync(stream, overwrite: true);
                    }

                    uploadedImageUrl = blobClient.Uri.ToString();
                }

                var recipe = new Recipe
                {
                    RecipeName = request.RecipeName,
                    RecipeDescription = request.Description,
                    AuthorId = authorId,
                    RecipeServings = request.Servings?.ToString(),
                    CookTimeMin = request.CookingTime,
                    PrepTimeMin = 0,
                    RecipeType = request.RecipeType ?? "Main Course",
                    RecipeImage = uploadedImageUrl,
                    RecipeURL = null
                };

                var recipeId = await _repository.AddRecipeAsync(recipe);

                if (request.Ingredients?.Any() == true)
                {
                    await _repository.AddRecipeIngredientsAsync(recipeId, request.Ingredients);
                }

                if (request.Instructions?.Any() == true)
                {
                    await _repository.AddRecipeInstructionsAsync(recipeId, request.Instructions);
                }

                if (request.Calories.HasValue || request.Protein.HasValue || request.Carbs.HasValue || request.Fat.HasValue)
                {
                    await _repository.AddRecipeNutritionAsync(recipeId, request.Calories, request.Protein, request.Carbs, request.Fat);
                }

                return Json(new { success = true, recipeId, message = "Recipe added successfully!" });
            }
            catch (Exception)
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

        private async Task<(List<Recipe> recipes, string pageTitle)> GetRecipesAsync(string? keywords, string? source)
        {
            Console.WriteLine($"[DEBUG] GetRecipesAsync called with keywords: '{keywords}', source: '{source}'");
            
            if (string.IsNullOrEmpty(keywords) && string.IsNullOrEmpty(source))
            {
                Console.WriteLine("[DEBUG] No keywords or source, getting all recipes");
                var allRecipes = await _repository.GetAllRecipesAsync();
                Console.WriteLine($"[DEBUG] GetAllRecipesAsync returned {allRecipes.Count} recipes");
                return (allRecipes, "All Recipes");
            }

            if (!string.IsNullOrEmpty(source))
            {
                Console.WriteLine($"[DEBUG] Source filter detected: '{source}', calling SearchRecipesByModeAndKeywordsAsync");
                var sourceRecipes = await _repository.SearchRecipesByModeAndKeywordsAsync(keywords ?? "", source);
                Console.WriteLine($"[DEBUG] SearchRecipesByModeAndKeywordsAsync returned {sourceRecipes.Count} recipes");
                return (sourceRecipes, $"{source} Recipes");
            }

            Console.WriteLine($"[DEBUG] Keywords only: '{keywords}', calling SearchRecipesByIngredientsAsync");
            var keywordRecipes = await _repository.SearchRecipesByIngredientsAsync(keywords ?? "");
            Console.WriteLine($"[DEBUG] SearchRecipesByIngredientsAsync returned {keywordRecipes.Count} recipes");
            return (keywordRecipes, "Search Results");
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

        private async Task<RecipeNutritionFacts?> GetRecipeNutritionAsync(int recipeId)
        {
            var recipeDetails = await _repository.GetRecipeDetailsWithNutritionAsync(recipeId);
            return recipeDetails?.NutritionFacts;
        }

        [HttpGet("SearchByModeAndKeywords")]
        public async Task<IActionResult> SearchRecipesByModeAndKeywords(string? keywords = "", string? mode = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keywords) && string.IsNullOrWhiteSpace(mode))
                {
                    return Json(new { success = true, recipes = new List<object>() });
                }

                var recipes = await _repository.SearchRecipesByModeAndKeywordsAsync(keywords ?? "", mode);

                var recipeResults = recipes.Select(recipe => new
                {
                    id = recipe.RecipeID,
                    name = recipe.RecipeName,
                    description = recipe.RecipeDescription,
                    author = recipe.Author?.Name ?? "Unknown Author",
                    type = recipe.RecipeType,
                    servings = recipe.RecipeServings,
                    cookTime = recipe.CookTimeMin,
                    prepTime = recipe.PrepTimeMin,
                    totalTime = recipe.TotalTimeMin,
                    image = recipe.RecipeImage
                }).ToList();

                return Json(new { success = true, recipes = recipeResults });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error searching recipes: {ex.Message}" });
            }
        }
    }
}