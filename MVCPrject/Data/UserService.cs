using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MVCPrject.Models;
using LinqKit;

namespace MVCPrject.Data
{
    public class UserService
    {
        private readonly DBContext _dbContext;
        private readonly ILogger<UserService> _logger;
        public UserService(DBContext dBContext, ILogger<UserService> logger)
        {
            _dbContext = dBContext;
            _logger = logger;
        }


        public async Task<int> GetLikeCountAsync(int recipeId)
        {
            return await _dbContext.RecipeLikes
                .CountAsync(rl => rl.RecipeID == recipeId);
        }

        public async Task<int> AddLike(int recipeId, string userid)
        {
            // Check if the user has already liked this recipe
            var existingLike = await _dbContext.RecipeLikes
                .FirstOrDefaultAsync(rl => rl.RecipeID == recipeId && rl.UserID == userid);

            // If user already liked this recipe, return 0 (no action taken)
            if (existingLike != null)
            {
                return 0;
            }

            // Create new like
            var newLike = new RecipeLikes
            {
                RecipeID = recipeId,
                UserID = userid,
                LikedAt = DateTime.UtcNow
            };

            _dbContext.RecipeLikes.Add(newLike);
            await _dbContext.SaveChangesAsync();

            // Return 1 to indicate successful like addition
            return 1;
        }

        public async Task<int> RemoveLike(int recipeId, string userid)
        {
            // Find the existing like
            var existingLike = await _dbContext.RecipeLikes
                .FirstOrDefaultAsync(rl => rl.RecipeID == recipeId && rl.UserID == userid);

            // If no like exists, return 0 (no action taken)
            if (existingLike == null)
            {
                return 0;
            }

            // Remove the like
            _dbContext.RecipeLikes.Remove(existingLike);
            await _dbContext.SaveChangesAsync();

            // Return 1 to indicate successful like removal
            return 1;
        }

        public async Task<bool> FollowUserAsync(string followerId, string followeeId)
        {
            if (followerId == followeeId) return false; // Prevent self-follow

            var exists = await _dbContext.Set<Models.Follows>()
                .AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId);
            if (exists) return false;

            var follow = new Models.Follows
            {
                FollowerId = followerId,
                FolloweeId = followeeId,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Set<Models.Follows>().Add(follow);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnfollowUserAsync(string followerId, string followeeId)
        {
            var follow = await _dbContext.Set<Models.Follows>()
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId);
            if (follow == null) return false;
            _dbContext.Set<Models.Follows>().Remove(follow);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Follow a user by email - converts emails to user IDs and calls FollowUserAsync
        /// </summary>
        public async Task<(bool success, string message)> FollowUserByEmailAsync(string followerEmail, string followeeEmail)
        {
            if (string.IsNullOrEmpty(followerEmail) || string.IsNullOrEmpty(followeeEmail))
                return (false, "User emails required.");

            var follower = _dbContext.Users.FirstOrDefault(u => u.UserName == followerEmail);
            var followee = _dbContext.Users.FirstOrDefault(u => u.UserName == followeeEmail);

            if (follower == null || followee == null)
                return (false, "User(s) not found.");

            var result = await FollowUserAsync(follower.Id, followee.Id);
            return result ? (true, "Successfully followed user.") : (false, "Unable to follow user.");
        }

        /// <summary>
        /// Unfollow a user by email - converts emails to user IDs and calls UnfollowUserAsync
        /// </summary>
        public async Task<(bool success, string message)> UnfollowUserByEmailAsync(string followerEmail, string followeeEmail)
        {
            if (string.IsNullOrEmpty(followerEmail) || string.IsNullOrEmpty(followeeEmail))
                return (false, "User emails required.");

            var follower = _dbContext.Users.FirstOrDefault(u => u.UserName == followerEmail);
            var followee = _dbContext.Users.FirstOrDefault(u => u.UserName == followeeEmail);

            if (follower == null || followee == null)
                return (false, "User(s) not found.");

            var result = await UnfollowUserAsync(follower.Id, followee.Id);
            return result ? (true, "Successfully unfollowed user.") : (false, "Unable to unfollow user.");
        }

        /// <summary>
        /// Check if one user is following another by email
        /// </summary>
        public async Task<bool> IsFollowingByEmailAsync(string followerEmail, string followeeEmail)
        {
            if (string.IsNullOrEmpty(followerEmail) || string.IsNullOrEmpty(followeeEmail))
                return false;

            var follower = _dbContext.Users.FirstOrDefault(u => u.UserName == followerEmail);
            var followee = _dbContext.Users.FirstOrDefault(u => u.UserName == followeeEmail);

            if (follower == null || followee == null)
                return false;

            return await _dbContext.Set<Models.Follows>()
                .AnyAsync(f => f.FollowerId == follower.Id && f.FolloweeId == followee.Id);
        }

        /// <summary>
        /// Get user by various lookup methods (ID, name, email, username)
        /// </summary>
        public User? GetUser(string? id = null, string? name = null, string? email = null, string? username = null)
        {
            var lookup = id ?? name ?? email ?? username;
            if (string.IsNullOrEmpty(lookup))
                return null;

            // Try to find by ID first (most specific)
            if (!string.IsNullOrEmpty(id))
            {
                return _dbContext.Users.FirstOrDefault(u => u.Id == id);
            }
            // Then try by name
            else if (!string.IsNullOrEmpty(name))
            {
                return _dbContext.Users.FirstOrDefault(u => u.Name == name);
            }
            // Then try by email/username
            else
            {
                return _dbContext.Users.FirstOrDefault(u => u.UserName == lookup || u.Email == lookup);
            }
        }
        /// <summary>
        /// Get the number of followers for a user by email
        /// </summary>
        public async Task<int> GetFollowerCountByEmailAsync(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
                return 0;

            var user = _dbContext.Users.FirstOrDefault(u => u.UserName == userEmail);
            if (user == null)
                return 0;

            return await _dbContext.Set<Models.Follows>()
                .CountAsync(f => f.FolloweeId == user.Id);
        }

        /// <summary>
        /// Get the number of users that a user is following by email
        /// </summary>
        public async Task<int> GetFollowingCountByEmailAsync(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
                return 0;

            var user = _dbContext.Users.FirstOrDefault(u => u.UserName == userEmail);
            if (user == null)
                return 0;

            return await _dbContext.Set<Models.Follows>()
                .CountAsync(f => f.FollowerId == user.Id);
        }

        /// <summary>
        /// Get the number of recipes created by a user by email
        /// </summary>
        public async Task<int> GetRecipeCountByEmailAsync(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
                return 0;

            var user = _dbContext.Users.FirstOrDefault(u => u.UserName == userEmail);
            if (user == null)
                return 0;

            return await _dbContext.Recipes
                .CountAsync(r => r.AuthorId == user.Id);
        }

        /// <summary>
        /// Get recipes created by a user by email
        /// </summary>
        public async Task<List<Recipe>> GetRecipesByEmailAsync(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
                return new List<Recipe>();

            var user = _dbContext.Users.FirstOrDefault(u => u.UserName == userEmail);
            if (user == null)
                return new List<Recipe>();

            return await _dbContext.Recipes
                .Include(r => r.Author)
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .Where(r => r.AuthorId == user.Id)
                .OrderByDescending(r => r.RecipeID)
                .ToListAsync();
        }

        /// <summary>
        /// Get recipes created by a user by user ID
        /// </summary>
        public async Task<List<Recipe>> GetRecipesByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return new List<Recipe>();

            return await _dbContext.Recipes
                .Include(r => r.Author)
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .Where(r => r.AuthorId == userId)
                .OrderByDescending(r => r.RecipeID)
                .ToListAsync();
        }

        /// <summary>
        /// Get followers of a user by email
        /// </summary>
        public async Task<List<User>> GetFollowersByEmailAsync(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
                return new List<User>();

            var user = _dbContext.Users.FirstOrDefault(u => u.UserName == userEmail);
            if (user == null)
                return new List<User>();

            return await _dbContext.Set<Models.Follows>()
                .Where(f => f.FolloweeId == user.Id)
                .Join(_dbContext.Users, f => f.FollowerId, u => u.Id, (f, u) => u)
                .ToListAsync();
        }

        /// <summary>
        /// Get users that a user is following by email
        /// </summary>
        public async Task<List<User>> GetFollowingByEmailAsync(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
                return new List<User>();

            var user = _dbContext.Users.FirstOrDefault(u => u.UserName == userEmail);
            if (user == null)
                return new List<User>();

            return await _dbContext.Set<Models.Follows>()
                .Where(f => f.FollowerId == user.Id)
                .Join(_dbContext.Users, f => f.FolloweeId, u => u.Id, (f, u) => u)
                .ToListAsync();
        }

        public async Task<bool> DeleteRecipeAsync(int recipeId)
        {
            try
            {
                var recipeCard = await _dbContext.Recipes.FindAsync(recipeId);
                if (recipeCard == null) return false;

                _dbContext.Recipes.Remove(recipeCard);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}