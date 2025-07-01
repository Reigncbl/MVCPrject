using System.Data.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
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

            // Get initial data for faster page load
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

        /// <summary>
        /// Follow another user by email (UserName field is the email address)
        /// Example: followerEmail=J024@gmail.com, followeeEmail=another@email.com
        /// </summary>
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

        /// <summary>
        /// Unfollow another user by email (UserName field is the email address)
        /// Example: followerEmail=J024@gmail.com, followeeEmail=another@email.com
        /// </summary>
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

        /// <summary>
        /// View another user's profile by user ID, name, email, or username
        /// Examples: 
        /// - /Profile/ProfileOthers?id=0b4b92d2-12fc-4738-bf0e-af1010bc57a7
        /// - /Profile/ProfileOthers?name=Paul
        /// - /Profile/ProfileOthers?email=J024@gmail.com
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ProfileOthers(string id, string name, string email, string username)
        {
            var user = _userService.GetUser(id, name, email, username);
            if (user == null)
                return NotFound();

            // Check if the user being viewed is the same as the current logged-in user
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && user != null && currentUser.Id == user.Id)
                {
                    // Redirect to own profile instead of ProfileOthers
                    return RedirectToAction("Profile");
                }
            }

            return View("ProfileOthers", user);
        }

        /// <summary>
        /// Check if current user is following another user by email
        /// </summary>
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

        /// <summary>
        /// Get user statistics (followers, following, recipes count)
        /// </summary>
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

        /// <summary>
        /// Get recipes created by a user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserRecipes(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
                return Json(new { success = false, recipes = new List<object>() });

            try
            {
                var recipes = await _userService.GetRecipesByEmailAsync(userEmail);

                var recipeResults = recipes.Select(recipe => new
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
                    ingredientCount = recipe.Ingredients?.Count ?? 0,
                    instructionCount = recipe.Instructions?.Count ?? 0
                }).ToList();

                return Json(new { success = true, recipes = recipeResults });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user recipes for email: {UserEmail}", userEmail);
                return Json(new { success = false, recipes = new List<object>() });
            }
        }

        /// <summary>
        /// Get followers of a user
        /// </summary>
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
                    profileImage = "/img/image.png"
                }).ToList();

                return Json(new { success = true, followers = followerResults });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting followers for email: {UserEmail}", userEmail);
                return Json(new { success = false, followers = new List<object>() });
            }
        }

        /// <summary>
        /// Get users that a user is following
        /// </summary>
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
                    profileImage = "/img/image.png"
                }).ToList();

                return Json(new { success = true, following = followingResults });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting following for email: {UserEmail}", userEmail);
                return Json(new { success = false, following = new List<object>() });
            }
        }
    }

    public class FollowRequest
    {
        public string followerEmail { get; set; } = string.Empty;
        public string followeeEmail { get; set; } = string.Empty;
    }
}