using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MVCPrject.Models;

namespace MVCPrject.Controllers
{
    public class LandingController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public LandingController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<string> Getinfo()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Debug: Show what's in the claims
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    // Try to find user by the ID from claims
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var userById = await _userManager.FindByIdAsync(userId);
                        if (userById != null)
                        {
                            return $"Found user by ID from claims. ID: '{userById.Id}', Name: '{userById.Name}', UserName: '{userById.UserName}', Email: '{userById.Email}'";
                        }
                    }

                    return $"User is authenticated but user object is null. Claims - UserId: '{userId}', UserName: '{userName}', Email: '{email}'";
                }

                return $"User ID: '{user.Id ?? "NULL"}', User Name: '{user.Name ?? "NULL"}', UserName: '{user.UserName ?? "NULL"}', Email: '{user.Email ?? "NULL"}'";
            }
            else
            {
                return "User not authenticated";
            }
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
                // Check if user exists first
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

                // Add more detailed error information
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

    }
}