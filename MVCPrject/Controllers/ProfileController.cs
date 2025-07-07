using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MVCPrject.Data;
using MVCPrject.Models;
using Azure.Storage.Blobs;

namespace MVCPrject.Controllers
{
    public class ProfileController : Controller
    {
        private readonly DBContext _dbContext;
        private readonly UserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ProfileController> _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public ProfileController(DBContext dbContext, UserService userService, UserManager<User> userManager, ILogger<ProfileController> logger, BlobServiceClient blobServiceClient)
        {
            _dbContext = dbContext;
            _userService = userService;
            _userManager = userManager;
            _logger = logger;
            _blobServiceClient = blobServiceClient;
        }

        public async Task<IActionResult> Profile()
        {
            _logger.LogInformation("Profile page accessed.");

            if (!User.Identity?.IsAuthenticated == true)
            {
                _logger.LogWarning("User is not authenticated. Redirecting to login.");
                return RedirectToAction("Login", "Landing");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Current user not found. Redirecting to login.");
                return RedirectToAction("Login", "Landing");
            }

            _logger.LogInformation("User {UserName} authenticated.", currentUser.UserName);

            var followers = await _userService.GetFollowerCountByEmailAsync(currentUser.UserName ?? "");
            var following = await _userService.GetFollowingCountByEmailAsync(currentUser.UserName ?? "");
            var recipes = await _userService.GetRecipeCountByEmailAsync(currentUser.UserName ?? "");

            _logger.LogInformation("User {UserName} stats: Followers={Followers}, Following={Following}, Recipes={Recipes}",
                currentUser.UserName, followers, following, recipes);

            var viewModel = new ProfileViewModel
            {
                User = currentUser,
                FollowerCount = followers,
                FollowingCount = following,
                RecipeCount = recipes
            };

            _logger.LogInformation("ProfileViewModel created for user {UserName}.", currentUser.UserName);

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Json(new { success = false, message = "User not found" });

                // Validate input
                var validationError = ValidateProfileRequest(request);
                if (validationError != null)
                    return Json(new { success = false, message = validationError });

                // Check username availability
                if (request.Username != currentUser.UserName)
                {
                    var existingUser = await _userManager.FindByNameAsync(request.Username);
                    if (existingUser != null && existingUser.Id != currentUser.Id)
                        return Json(new { success = false, message = "Username is already taken" });
                }

                // Track changes for logging
                var changes = new List<string>();
                var oldDisplayName = currentUser.Name;
                var oldUsername = currentUser.UserName;
                var oldProfileImageUrl = currentUser.ProfileImageUrl;

                // Handle image upload
                if (!string.IsNullOrEmpty(request.ProfileImageBase64))
                {
                    var imageUrl = await UploadProfileImageAsync(currentUser.Id, request.ProfileImageBase64);
                    if (imageUrl == null)
                        return Json(new { success = false, message = "Failed to upload image" });

                    currentUser.ProfileImageUrl = imageUrl;
                    changes.Add($"Profile image updated from '{oldProfileImageUrl ?? "none"}' to '{imageUrl}'");
                    _logger.LogInformation("Profile image updated for user {UserId}: {OldImage} -> {NewImage}",
                        currentUser.Id, oldProfileImageUrl ?? "none", imageUrl);
                }

                // Update user data
                if (currentUser.Name != request.DisplayName.Trim())
                {
                    currentUser.Name = request.DisplayName.Trim();
                    changes.Add($"Display name changed from '{oldDisplayName}' to '{currentUser.Name}'");
                    _logger.LogInformation("Display name updated for user {UserId}: '{OldName}' -> '{NewName}'",
                        currentUser.Id, oldDisplayName, currentUser.Name);
                }

                if (currentUser.UserName != request.Username.Trim())
                {
                    currentUser.UserName = request.Username.Trim();
                    changes.Add($"Username changed from '{oldUsername}' to '{currentUser.UserName}'");
                    _logger.LogInformation("Username updated for user {UserId}: '{OldUsername}' -> '{NewUsername}'",
                        currentUser.Id, oldUsername, currentUser.UserName);
                }

                var result = await _userManager.UpdateAsync(currentUser);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to update profile for user {UserId}. Errors: {Errors}",
                        currentUser.Id, errors);
                    return Json(new { success = false, message = errors });
                }

                // Log successful update with all changes
                if (changes.Any())
                {
                    _logger.LogInformation("Profile successfully updated for user {UserId} ({Username}). Changes: {Changes}",
                        currentUser.Id, currentUser.UserName, string.Join("; ", changes));
                }
                else
                {
                    _logger.LogInformation("Profile update request processed for user {UserId} ({Username}) but no changes were made",
                        currentUser.Id, currentUser.UserName);
                }

                return Json(new
                {
                    success = true,
                    message = "Profile updated successfully!",
                    data = new
                    {
                        displayName = currentUser.Name,
                        username = currentUser.UserName,
                        profileImageUrl = currentUser.ProfileImageUrl
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return Json(new { success = false, message = "An unexpected error occurred. Please try again." });
            }
        }

        private string? ValidateProfileRequest(UpdateProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Username))
                return "Display name and username are required";

            if (request.DisplayName.Length > 100)
                return "Display name must be 100 characters or less";

            if (request.Username.Length < 3 || request.Username.Length > 50)
                return "Username must be between 3 and 50 characters";

            return null;
        }

        private async Task<string?> UploadProfileImageAsync(string userId, string base64Image)
        {
            try
            {
                // Validate file size (5MB max)
                var imageBytes = Convert.FromBase64String(base64Image);
                const long maxFileSize = 5 * 1024 * 1024;
                if (imageBytes.Length > maxFileSize)
                    return null;

                var containerClient = _blobServiceClient.GetBlobContainerClient("profiles");
                await containerClient.CreateIfNotExistsAsync();

                var blobName = $"profile-{userId}-{Guid.NewGuid()}.jpg";
                var blobClient = containerClient.GetBlobClient(blobName);

                using (var stream = new MemoryStream(imageBytes))
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                await blobClient.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders
                {
                    ContentType = "image/jpeg"
                });

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload profile image for user {UserId}", userId);
                return null;
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

                var recipeResults = recipes.Select(recipe =>
                {
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

                return Json(new
                {
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

    public class UpdateProfileRequest
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? ProfileImageBase64 { get; set; }
    }
}