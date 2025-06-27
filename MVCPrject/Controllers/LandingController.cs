using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MVCPrject.Models;
using MVCPrject.Services;

namespace MVCPrject.Controllers
{
    public class LandingController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IUserCacheService _userCacheService;

        public LandingController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IUserCacheService userCacheService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userCacheService = userCacheService;
        }

        public async Task<string> GetInfo()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return "User not authenticated";
            }

            var userInfo = await _userCacheService.GetUserInfoAsync(User);
            if (userInfo == null)
            {
                return "User is authenticated but user data could not be retrieved";
            }

            return $"User ID: '{userInfo.Id}', Name: '{userInfo.Name ?? "NULL"}', UserName: '{userInfo.UserName ?? "NULL"}', Email: '{userInfo.Email ?? "NULL"}'";
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, $"No user found with email: {model.Email}");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Home", "Home");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Account is locked out.");
                }
                else if (result.RequiresTwoFactor)
                {
                    ModelState.AddModelError(string.Empty, "Two-factor authentication required.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt - incorrect password.");
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Landing");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Clear user cache before signing out
            var user = await _userCacheService.GetCurrentUserAsync(User);
            if (user != null)
            {
                _userCacheService.ClearUserCache(user.Id);
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Index");
        }
    }
}