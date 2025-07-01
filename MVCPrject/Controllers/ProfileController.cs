using System.Data.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MVCPrject.Data;
using MVCPrject.Models;

namespace MVCPrject.Controllers
{
    public class ProfileController : Controller
    {

        private readonly DBContext _dbContext;
        private readonly UserService _userService;
        private readonly UserManager<User> _userManager;

        public ProfileController(DBContext dbContext, UserService userService, UserManager<User> userManager)
        {
            _dbContext = dbContext;
            _userService = userService;
            _userManager = userManager;
        }
        public IActionResult Profile()
        {
            return View();
        }



        /// <summary>
        /// Follow another user by email (UserName field is the email address)
        /// Example: followerEmail=J024@gmail.com, followeeEmail=another@email.com
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Follow(string followerEmail, string followeeEmail)
        {
            if (string.IsNullOrEmpty(followerEmail) || string.IsNullOrEmpty(followeeEmail))
                return Json(new { success = false, message = "User emails required." });
            var follower = _dbContext.Users.FirstOrDefault(u => u.UserName == followerEmail);
            var followee = _dbContext.Users.FirstOrDefault(u => u.UserName == followeeEmail);
            if (follower == null || followee == null)
                return Json(new { success = false, message = "User(s) not found." });
            var result = await _userService.FollowUserAsync(follower.Id, followee.Id);
            if (result)
                return Json(new { success = true });
            return Json(new { success = false, message = "Unable to follow user." });
        }

        /// <summary>
        /// Unfollow another user by email (UserName field is the email address)
        /// Example: followerEmail=J024@gmail.com, followeeEmail=another@email.com
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UnFollow(string followerEmail, string followeeEmail)
        {
            if (string.IsNullOrEmpty(followerEmail) || string.IsNullOrEmpty(followeeEmail))
                return Json(new { success = false, message = "User emails required." });
            var follower = _dbContext.Users.FirstOrDefault(u => u.UserName == followerEmail);
            var followee = _dbContext.Users.FirstOrDefault(u => u.UserName == followeeEmail);
            if (follower == null || followee == null)
                return Json(new { success = false, message = "User(s) not found." });
            var result = await _userService.UnfollowUserAsync(follower.Id, followee.Id);
            if (result)
                return Json(new { success = true });
            return Json(new { success = false, message = "Unable to unfollow user." });
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
            var lookup = id ?? name ?? email ?? username;
            if (string.IsNullOrEmpty(lookup))
                return NotFound();
            
            User user = null;
            
            // Try to find by ID first (most specific)
            if (!string.IsNullOrEmpty(id))
            {
                user = _dbContext.Users.FirstOrDefault(u => u.Id == id);
            }
            // Then try by name
            else if (!string.IsNullOrEmpty(name))
            {
                user = _dbContext.Users.FirstOrDefault(u => u.Name == name);
            }
            // Then try by email/username
            else
            {
                user = _dbContext.Users.FirstOrDefault(u => u.UserName == lookup || u.Email == lookup);
            }
            
            if (user == null)
                return NotFound();
            
            // Check if the user being viewed is the same as the current logged-in user
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && currentUser.Id == user.Id)
                {
                    // Redirect to own profile instead of ProfileOthers
                    return RedirectToAction("Profile");
                }
            }
            
            return View("ProfileOthers", user);
        }
    }
}
