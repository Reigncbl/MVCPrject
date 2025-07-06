using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
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
            _logger.LogInformation("UpdateProfile: Starting profile update for user");
            
            try
            {
                // Get current user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("UpdateProfile: User not found");
                    return Json(new { success = false, message = "User not found" });
                }

                _logger.LogInformation("UpdateProfile: User {UserId} updating profile - DisplayName: '{DisplayName}', Username: '{Username}', HasImage: {HasImage}",
                    currentUser.Id, request.DisplayName, request.Username, !string.IsNullOrEmpty(request.ProfileImageBase64));

                // Simple validation
                if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Username))
                {
                    _logger.LogWarning("UpdateProfile: Validation failed - empty fields");
                    return Json(new { success = false, message = "Display name and username are required" });
                }

                if (request.DisplayName.Length > 100 || request.Username.Length < 3 || request.Username.Length > 50)
                {
                    _logger.LogWarning("UpdateProfile: Validation failed - invalid length");
                    return Json(new { success = false, message = "Invalid field lengths" });
                }

                // Check username availability
                if (request.Username != currentUser.UserName)
                {
                    var existingUser = await _userManager.FindByNameAsync(request.Username);
                    if (existingUser != null && existingUser.Id != currentUser.Id)
                    {
                        _logger.LogWarning("UpdateProfile: Username '{Username}' already taken", request.Username);
                        return Json(new { success = false, message = "Username is already taken" });
                    }
                }

                // Handle image upload if provided
                if (!string.IsNullOrEmpty(request.ProfileImageBase64))
                {
                    _logger.LogInformation("UpdateProfile: Processing image upload");
                    
                    var imageUrl = await UploadProfileImageAsync(currentUser.Id, request.ProfileImageBase64);
                    if (imageUrl != null)
                    {
                        currentUser.ProfileImageUrl = imageUrl;
                        _logger.LogInformation("UpdateProfile: Image uploaded successfully to {ImageUrl}", imageUrl);
                    }
                    else
                    {
                        _logger.LogError("UpdateProfile: Image upload failed");
                        return Json(new { success = false, message = "Failed to upload image" });
                    }
                }

                // Update user data
                currentUser.Name = request.DisplayName.Trim();
                currentUser.UserName = request.Username.Trim();

                // Save changes
                var result = await _userManager.UpdateAsync(currentUser);
                if (result.Succeeded)
                {
                    _logger.LogInformation("UpdateProfile: Profile updated successfully for user {UserId}", currentUser.Id);
                    return Json(new { 
                        success = true, 
                        message = "Profile updated successfully!",
                        data = new {
                            displayName = currentUser.Name,
                            username = currentUser.UserName,
                            profileImageUrl = currentUser.ProfileImageUrl
                        }
                    });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("UpdateProfile: Failed to save changes - {Errors}", errors);
                    return Json(new { success = false, message = errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateProfile: Unexpected error occurred");
                return Json(new { success = false, message = "An unexpected error occurred. Please try again." });
            }
        }

        private async Task<string?> UploadProfileImageAsync(string userId, string base64Image)
        {
            try
            {
                _logger.LogInformation("UploadProfileImage: Starting upload for user {UserId}", userId);

                // Convert base64 to bytes
                var imageBytes = Convert.FromBase64String(base64Image);
                
                // Validate file size (5MB max)
                const long maxFileSize = 5 * 1024 * 1024;
                if (imageBytes.Length > maxFileSize)
                {
                    _logger.LogWarning("UploadProfileImage: File too large - {FileSize} bytes", imageBytes.Length);
                    return null;
                }

                // Setup Azure Blob Storage
                var containerClient = _blobServiceClient.GetBlobContainerClient("profiles");
                await containerClient.CreateIfNotExistsAsync();

                var blobName = $"profile-{userId}-{Guid.NewGuid()}.jpg";
                var blobClient = containerClient.GetBlobClient(blobName);

                _logger.LogInformation("UploadProfileImage: Uploading to blob {BlobName}", blobName);

                // Upload image
                using (var stream = new MemoryStream(imageBytes))
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                // Set content type
                await blobClient.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders
                {
                    ContentType = "image/jpeg"
                });

                var imageUrl = blobClient.Uri.ToString();
                _logger.LogInformation("UploadProfileImage: Upload successful - {ImageUrl}", imageUrl);
                
                return imageUrl;
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "UploadProfileImage: Invalid base64 format");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadProfileImage: Upload failed for user {UserId}", userId);
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