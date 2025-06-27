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
        public UserService(DBContext dBContext)
        {
            _dbContext = dBContext;
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

    }
}