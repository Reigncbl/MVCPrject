<<<<<<< Updated upstream
=======
using Microsoft.AspNetCore.Authorization;
>>>>>>> Stashed changes
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MVCPrject.Data;
using MVCPrject.Models;

namespace MVCPrject.Controllers
{
    public class ProfileController : Controller
    {
        private readonly DBContext _dbContext;
        private readonly UserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(DBContext dbContext, UserService userService, UserManager<User> userManager, ILogger<ProfileController> logger)
        {
            _dbContext = dbContext;
            _userService = userService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Profile()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Login", "Landing");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Landing");
            }

            var followers = await _userService.GetFollowerCountByEmailAsync(currentUser.UserName ?? "");
            var following = await _userService.GetFollowingCountByEmailAsync(currentUser.UserName ?? "");
            var recipes = await _userService.GetRecipeCountByEmailAsync(currentUser.UserName ?? "");

            var viewModel = new ProfileViewModel
            {
                User = currentUser,
                FollowerCount = followers,
                FollowingCount = following,
                RecipeCount = recipes
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize] // Now recognized with the correct namespace
        public async Task<IActionResult> UpdateProfile([FromForm] string displayName, string username, IFormFile profileImage)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("UpdateProfile: User not found");
                return Json(new { success = false, message = "User not found" });
            }

            if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("UpdateProfile: DisplayName or Username is empty");
                return Json(new { success = false, message = "Display name and username are required" });
            }

            if (profileImage != null)
            {
                try
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profileImage.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }
                    currentUser.ProfileImageUrl = "/img/" + fileName;
                    _logger.LogInformation("UpdateProfile: Profile image uploaded to {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "UpdateProfile: Error uploading profile image");
                    return Json(new { success = false, message = "Error uploading profile image" });
                }
            }

            currentUser.Name = displayName;
            currentUser.UserName = username;

            var result = await _userManager.UpdateAsync(currentUser);
            if (result.Succeeded)
            {
                _logger.LogInformation("UpdateProfile: Profile updated successfully for user {UserId}", currentUser.Id);
                return Json(new { success = true });
            }
            else
            {
                _logger.LogWarning("UpdateProfile: Failed to update profile for user {UserId}. Errors: {Errors}", 
                    currentUser.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Follow([FromBody] FollowRequest request)
        {
            _logger.LogInformation("Follow endpoint called with followerEmail: {FollowerEmail}, followeeEmail: {FolloweeEmail}",
                request?.followerEmail, request?.followeeEmail);

            if (request == null)
            {
                _logger.LogWarning("Follow request is null");
                return Json(new { success = false, message = "Invalid request" });
            }

            var (success, message) = await _userService.FollowUserByEmailAsync(request.followerEmail, request.followeeEmail);

            _logger.LogInformation("Follow result: Success={Success}, Message={Message}", success, message);
            return Json(new { success, message });
        }

        [HttpPost]
        public async Task<IActionResult> UnFollow([FromBody] FollowRequest request)
        {
            _logger.LogInformation("UnFollow endpoint called with followerEmail: {FollowerEmail}, followeeEmail: {FolloweeEmail}",
                request?.followerEmail, request?.followeeEmail);

            if (request == null)
            {
                _logger.LogWarning("UnFollow request is null");
                return Json(new { success = false, message = "Invalid request" });
            }

            var (success, message) = await _userService.UnfollowUserByEmailAsync(request.followerEmail, request.followeeEmail);

            _logger.LogInformation("UnFollow result: Success={Success}, Message={Message}", success, message);
            return Json(new { success, message });
        }

        [HttpGet]
        public async Task<IActionResult> ProfileOthers(string id, string name, string email, string username)
        {
            var user = _userService.GetUser(id, name, email, username);
            if (user == null)
                return NotFound();

            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && user != null && currentUser.Id == user.Id)
                {
                    return RedirectToAction("Profile");
                }
            }

            return View("ProfileOthers", user);
        }

        [HttpGet]
        public async Task<IActionResult> IsFollowing(string followeeEmail)
        {
            if (!User.Identity?.IsAuthenticated == true)
                return Json(new { isFollowing = false });

            var currentUserEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUserEmail))
                return Json(new { isFollowing = false });

            var isFollowing = await _userService.IsFollowingByEmailAsync(currentUserEmail, followeeEmail);
            return Json(new { isFollowing });
        }

        [HttpGet]
        public async Task<IActionResult> GetUserStats(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
                return Json(new { followers = 0, following = 0, recipes = 0 });

            var followers = await _userService.GetFollowerCountByEmailAsync(userEmail);
            var following = await _userService.GetFollowingCountByEmailAsync(userEmail);
            var recipes = await _userService.GetRecipeCountByEmailAsync(userEmail);

            return Json(new { followers, following, recipes });
        }

        [HttpGet]
        public async Task<IActionResult> GetUserRecipes(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
                return Json(new { success = false, recipes = new List<object>() });

            try
            {
                // Get user
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userEmail);
                if (user == null)
                    return Json(new { success = false, message = "User not found." });

                // Get recipes with includes
                var recipes = await _dbContext.Recipes
                    .Where(r => r.AuthorId == user.Id)
                    .Include(r => r.Author)
                    .Include(r => r.Ingredients)
                    .Include(r => r.Instructions)
                    .OrderByDescending(r => r.RecipeID)
                    .ToListAsync();

                // Get nutrition facts separately
                var recipeIds = recipes.Select(r => r.RecipeID).ToList();
                var nutritionFacts = await _dbContext.RecipeNutritionFacts
                    .Where(nf => recipeIds.Contains(nf.RecipeID ?? 0))
                    .ToDictionaryAsync(nf => nf.RecipeID ?? 0);

                _logger.LogInformation("Found {Count} recipes for user {UserEmail}", recipes.Count, userEmail);

                var recipeResults = recipes.Select(recipe => {
                    nutritionFacts.TryGetValue(recipe.RecipeID, out var currentNutrition);
                    
                    var calories = currentNutrition?.Calories;
                    _logger.LogInformation("Recipe {RecipeId} - {RecipeName}: Calories = \'{Calories}\', NutritionFacts exists = {HasNutrition}",
                        recipe.RecipeID,
                        recipe.RecipeName,
                        calories ?? "NULL",
                        currentNutrition != null);

                    return new
                    {
                        id = recipe.RecipeID,
                        name = recipe.RecipeName,
                        description = recipe.RecipeDescription,
                        image = recipe.RecipeImage,
                        author = recipe.Author?.Name ?? "Unknown Author",
                        authorEmail = recipe.Author?.UserName,
                        type = recipe.RecipeType,
                        servings = recipe.RecipeServings,
                        cookTime = recipe.CookTimeMin,
                        prepTime = recipe.PrepTimeMin,
                        totalTime = recipe.TotalTimeMin,
                        Calories = calories,
                        ingredientCount = recipe.Ingredients?.Count ?? 0,
                        instructionCount = recipe.Instructions?.Count ?? 0
                    };
                }).ToList();

                return Json(new { success = true, recipes = recipeResults });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user recipes for email: {UserEmail}", userEmail);
                return Json(new { success = false, recipes = new List<object>() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserFollowers(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
                return Json(new { success = false, followers = new List<object>() });

            try
            {
                var followers = await _userService.GetFollowersByEmailAsync(userEmail);

                var followerResults = followers.Select(user => new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.UserName,
                    profileImage = user.ProfileImageUrl ?? "/img/image.png" // Updated to use ProfileImageUrl
                }).ToList();

                return Json(new { success = true, followers = followerResults });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting followers for email: {UserEmail}", userEmail);
                return Json(new { success = false, followers = new List<object>() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserFollowing(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
                return Json(new { success = false, following = new List<object>() });

            try
            {
                var following = await _userService.GetFollowingByEmailAsync(userEmail);

                var followingResults = following.Select(user => new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.UserName,
                    profileImage = user.ProfileImageUrl ?? "/img/image.png" // Updated to use ProfileImageUrl
                }).ToList();

                return Json(new { success = true, following = followingResults });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting following for email: {UserEmail}", userEmail);
                return Json(new { success = false, following = new List<object>() });
            }
        }


        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TestNutritionData()
        {
            try
            {
                var totalNutritionFacts = await _dbContext.RecipeNutritionFacts.CountAsync();
                var nutritionFactsWithCalories = await _dbContext.RecipeNutritionFacts
                    .Where(n => !string.IsNullOrEmpty(n.Calories))
                    .CountAsync();
                
                var sampleNutritionFacts = await _dbContext.RecipeNutritionFacts
                    .Take(5)
                    .Select(n => new { n.RecipeID, n.Calories, n.NutritionFactsID })
                    .ToListAsync();

                var totalRecipes = await _dbContext.Recipes.CountAsync();

                return Json(new { 
                    totalRecipes,
                    totalNutritionFacts, 
                    nutritionFactsWithCalories, 
                    sampleNutritionFacts 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing nutrition facts");
                return Json(new { error = ex.Message });
            }
        }
    }

    public class FollowRequest
    {
        public string followerEmail { get; set; } = string.Empty;
        public string followeeEmail { get; set; } = string.Empty;
    }
}