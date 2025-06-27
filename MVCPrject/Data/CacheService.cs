using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using MVCPrject.Models;
using System.Security.Claims;

namespace MVCPrject.Services
{
    public interface IUserCacheService
    {
        Task<User?> GetCurrentUserAsync(ClaimsPrincipal userPrincipal);
        Task<string> GetUserDisplayNameAsync(ClaimsPrincipal userPrincipal);
        Task<UserInfo?> GetUserInfoAsync(ClaimsPrincipal userPrincipal);
        void ClearUserCache(string userId);
    }

    public class UserCacheService : IUserCacheService
    {
        private readonly UserManager<User> _userManager;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public UserCacheService(UserManager<User> userManager, IMemoryCache cache)
        {
            _userManager = userManager;
            _cache = cache;
        }

        public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal userPrincipal)
        {
            if (!userPrincipal.Identity?.IsAuthenticated == true)
                return null;

            // Try to get user ID from claims
            var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return null;

            // Check cache first
            var cacheKey = $"user_{userId}";
            if (_cache.TryGetValue(cacheKey, out User? cachedUser) && cachedUser != null)
            {
                return cachedUser;
            }

            // Try UserManager first
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user != null)
            {
                CacheUser(cacheKey, user);
                return user;
            }

            // Fallback to finding by ID
            user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                CacheUser(cacheKey, user);
                return user;
            }

            // Last resort: try by email
            var email = userPrincipal.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    CacheUser(cacheKey, user);
                    return user;
                }
            }

            return null;
        }

        public async Task<string> GetUserDisplayNameAsync(ClaimsPrincipal userPrincipal)
        {
            var user = await GetCurrentUserAsync(userPrincipal);
            return user?.Name ?? user?.UserName ?? "User";
        }

        public async Task<UserInfo?> GetUserInfoAsync(ClaimsPrincipal userPrincipal)
        {
            var user = await GetCurrentUserAsync(userPrincipal);
            if (user == null) return null;

            return new UserInfo
            {
                Id = user.Id,
                Name = user.Name,
                UserName = user.UserName,
                Email = user.Email,
                DisplayName = user.Name ?? user.UserName ?? "User"
            };
        }

        public void ClearUserCache(string userId)
        {
            var cacheKey = $"user_{userId}";
            _cache.Remove(cacheKey);
        }

        private void CacheUser(string cacheKey, User user)
        {
            _cache.Set(cacheKey, user, _cacheExpiration);
        }
    }


}