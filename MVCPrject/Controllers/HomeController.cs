using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Identity;
using MVCPrject.Data;
using MVCPrject.Models;
using MVCPrject.Services;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MVCPrject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DBContext _context;
        private readonly IDistributedCache _cache;
        private readonly UserManager<User> _userManager;
        private readonly SuggestionService _suggestionService;

        public HomeController(ILogger<HomeController> logger, DBContext context, IDistributedCache cache, UserManager<User> userManager, SuggestionService suggestionService)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
            _userManager = userManager;
            _suggestionService = suggestionService;
        }

        public async Task<IActionResult> Home()
        {
            List<Suggestion> suggestions = new List<Suggestion>();
            
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                var user = await _userManager.GetUserAsync(User);

                // If user is null, try to find by ID from claims
                if (user == null && !string.IsNullOrEmpty(userId))
                {
                    user = await _userManager.FindByIdAsync(userId);
                }

                // If still null, try to find by email
                if (user == null && !string.IsNullOrEmpty(email))
                {
                    user = await _userManager.FindByEmailAsync(email);
                }

                ViewBag.UserName = user?.Name ?? userName ?? email ?? "User";
                ViewBag.Name = user?.Name ?? userName ?? email ?? "User";
                ViewBag.id = user?.Id ?? userId;
                ViewBag.UserProfileImage = user?.ProfileImageUrl;

                // Get user's liked recipes
                if (!string.IsNullOrEmpty(userId))
                {
                    suggestions = await _suggestionService.GetUserSuggestionsAsync(userId);
                }
            }
            
            return View(suggestions);
        }

        // Reference Landing controller's Getinfo functionality
        [HttpGet]
        public async Task<string> GetUserInfo()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    // Same fallback as Landing controller
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var userById = await _userManager.FindByIdAsync(userId);
                        if (userById != null)
                        {
                            return $"Found user by ID from claims. ID: '{userById.Id}', Name: '{userById.Name}', UserName: '{userById.UserName}', Email: '{userById.Email}'";
                        }
                    }
                    return "User is authenticated but user object is null";
                }
                return $"User ID: '{user.Id}', User Name: '{user.Name ?? "NULL"}', UserName: '{user.UserName}', Email: '{user.Email}'";
            }
            return "User not authenticated";
        }

        // Reference to logout (redirect to Landing controller)
        public IActionResult Logout()
        {
            return RedirectToAction("Login", "Landing");
        }

        public IActionResult Newview()
        {
            return View();
        }

        public IActionResult Recipe()
        {
            return View();
        }

        public IActionResult CreateEditRecipe()
        {
            return View();
        }

        public IActionResult CreateEditRecipeForm(Recipe model)
        {
            _context.Recipes.Add(model);
            _context.SaveChanges();
            return RedirectToAction("Recipe");
        }


        [HttpGet("/suggestion")]
        public async Task<IActionResult> Suggestion()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var userLikes = await _context.RecipeLikes
                .Where(rl => rl.UserID == userId).Take(5)
                .ToListAsync();

            var suggestions = new List<Suggestion>();
            foreach (var like in userLikes)
            {
                var recipe = await _context.Recipes.FindAsync(like.RecipeID);
                suggestions.Add(new Suggestion
                {
                    recipeLikes = like,
                    recipe = recipe
                });
            }

            return Json(suggestions);
        }

        [HttpGet("/inspect-cache")]
        public async Task<IActionResult> InspectCache([FromQuery] string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return BadRequest("Please provide a cache key as ?key=...");

            var cachedData = await _cache.GetStringAsync(key);
            if (cachedData == null)
                return Ok($"Cache key: {key}\nValue: (not found)");

            return Content($"Cache key: {key}\nValue: {cachedData}", "text/plain");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
