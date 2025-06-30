using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MVCPrject.Models;
using MVCPrject.Services;
using System.Threading.Tasks;

namespace MVCPrject.Controllers
{
    public class LogoutController : Controller
    {
        private readonly IUserCacheService _userCacheService;
        private readonly UserManager<User> _userManager;

        public LogoutController(IUserCacheService userCacheService, UserManager<User> userManager)
        {
            _userCacheService = userCacheService;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Clear user cache before signing out
            var user = await _userCacheService.GetCurrentUserAsync(User);
            if (user != null)
            {
                _userCacheService.ClearUserCache(user.Id);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Landing");
        }
    }
}
