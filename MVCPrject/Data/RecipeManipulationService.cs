using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MVCPrject.Models;
using LinqKit;

namespace MVCPrject.Data
{
    public class RecipeManipulationService
    {
        private readonly DBContext _dbContext;
        private readonly IDistributedCache _cache;
        private readonly JsonSerializerOptions _jsonOptions;

        // Consider making these static readonly if they are truly constant and shared across all instances
        // private static readonly string[] CommonFilters = { "Dinner", "Breakfast", "Lunch", "Snack", "Dessert",
        //     "Main Course", "Appetizer", "Side Dish", "Soup", "Salad", "Healthy", "Vegetarian", "Vegan", "Comfort Food" };
        // private static readonly string[] Modes = { "user", "cookbook" };

        public RecipeManipulationService(DBContext dbContext, IDistributedCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
            _jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };
        }

        /// <summary>
        /// Retrieves data from cache, or if not found, fetches it using a factory function and then caches it.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve.</typeparam>
        /// <param name="cacheKey">The key used to store and retrieve data from the cache.</param>
        /// <param name="factory">A function that returns a Task of type T to fetch the data if it's not in the cache.</param>
        /// <param name="expiration">The TimeSpan indicating how long the data should be cached.</param>
        /// <returns>The retrieved or fetched data.</returns>
        private async Task<T?> GetOrSetCacheAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan expiration) where T : class
        {
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
            }

            var data = await factory();
            if (data != null)
            {
                var serializedData = JsonSerializer.Serialize(data, _jsonOptions);
                await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                });
            }
            return data;
        }

        /// <summary>
        /// Normalizes keywords by trimming, converting to lowercase, and joining them with commas in sorted order.
        /// </summary>
        /// <param name="keywords">The comma-separated string of keywords.</param>
        /// <returns>A normalized string of keywords.</returns>
        private string NormalizeKeywords(string keywords)
        {
            return string.Join(",", keywords.Split(',')
                .Select(k => k.Trim().ToLowerInvariant())
                .Where(k => !string.IsNullOrEmpty(k))
                .OrderBy(k => k));
        }

        /// <summary>
        /// Applies keyword search to a queryable collection of recipes.
        /// </summary>
        /// <param name="query">The IQueryable of recipes.</param>
        /// <param name="keywords">An array of keywords to search for.</param>
        /// <returns>The filtered IQueryable of recipes.</returns>
        private IQueryable<Recipe> ApplyKeywordSearch(IQueryable<Recipe> query, string[] keywords)
        {
            if (!keywords.Any()) return query;

            var predicate = PredicateBuilder.New<Recipe>();
            foreach (var keyword in keywords)
            {
                var k = keyword; // Capture keyword for closure
                predicate = predicate.Or(r =>
                    r.Ingredients.Any(i => EF.Functions.Like(i.IngredientName, $"%{k}%")) ||
                    EF.Functions.Like(r.RecipeName, $"%{k}%") ||
                    (r.Author != null && EF.Functions.Like(r.Author.Name, $"%{k}%")) ||
                    EF.Functions.Like(r.RecipeType, $"%{k}%"));
            }
            return query.Where(predicate);
        }

        /// <summary>
        /// Applies a mode filter (user or cookbook) to a queryable collection of recipes.
        /// </summary>
        /// <param name="query">The IQueryable of recipes.</param>
        /// <param name="mode">The mode to filter by ("user" or "cookbook").</param>
        /// <returns>The filtered IQueryable of recipes.</returns>
        private IQueryable<Recipe> ApplyModeFilter(IQueryable<Recipe> query, string? mode)
        {
            if (string.IsNullOrEmpty(mode)) return query;

            return mode.ToLower() switch
            {
                "user" => query.Where(r => r.RecipeMode != null && r.RecipeMode.ToLower() == "user"),
                "cookbook" => query.Where(r => r.RecipeMode == null || r.RecipeMode.ToLower() == "cookbook"),
                _ => query
            };
        }

        /// <summary>
        /// Fetches a single recipe's details with nutrition facts, utilizing caching for the recipe data.
        /// Nutrition facts are always fetched fresh from the database.
        /// </summary>
        /// <param name="id">The ID of the recipe to retrieve.</param>
        /// <returns>A RecipeDetailsViewModel containing the recipe and its nutrition facts, or null if not found.</returns>
        public async Task<RecipeDetailsViewModel?> GetRecipeDetailsWithNutritionAsync(int id)
        {
            var recipe = await GetOrSetCacheAsync($"recipeDetails_{id}", async () =>
            {
                return await _dbContext.Recipes
                    .Include(r => r.Ingredients)
                    .Include(r => r.Instructions)
                    .Include(r => r.Author)
                    .FirstOrDefaultAsync(r => r.RecipeID == id);
            }, TimeSpan.FromHours(10));

            if (recipe == null) return null;

            var nutritionFacts = await _dbContext.RecipeNutritionFacts
                .FirstOrDefaultAsync(nf => nf.RecipeID == id);

            return new RecipeDetailsViewModel
            {
                Recipe = recipe,
                NutritionFacts = nutritionFacts
            };
        }

        /// <summary>
        /// Fetches all recipes, utilizing caching.
        /// </summary>
        /// <returns>A list of all recipes.</returns>
        public async Task<List<Recipe>> GetAllRecipesAsync()
        {
            return await GetOrSetCacheAsync("recipeAllRecipes", async () =>
            {
                return await _dbContext.Recipes
                    .Include(r => r.Author)
                    .OrderBy(r => r.RecipeID)
                    .ToListAsync();
            }, TimeSpan.FromHours(10));
        }

        /// <summary>
        /// Searches for recipes by keywords in ingredients, recipe name, author name, or recipe type, utilizing caching.
        /// </summary>
        /// <param name="keywords">Comma-separated keywords to search for.</param>
        /// <returns>A list of recipes matching the keywords.</returns>
        public async Task<List<Recipe>> SearchRecipesByIngredientsAsync(string keywords)
        {
            var normalizedKeywords = NormalizeKeywords(keywords);
            var cacheKey = $"recipeSearchRecipes_{normalizedKeywords}";

            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var keywordList = keywords.Split(',')
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .ToArray();

                var query = ApplyKeywordSearch(_dbContext.Recipes.AsQueryable(), keywordList);

                return await query
                    .Include(r => r.Ingredients)
                    .Include(r => r.Instructions)
                    .Include(r => r.Author)
                    .OrderBy(r => r.RecipeID)
                    .AsSplitQuery()
                    .ToListAsync();
            }, TimeSpan.FromHours(24));
        }

        /// <summary>
        /// Searches for recipes by mode (user/cookbook) and keywords, utilizing caching.
        /// </summary>
        /// <param name="keywords">Comma-separated keywords to search for.</param>
        /// <param name="mode">Optional mode to filter by ("user" or "cookbook").</param>
        /// <returns>A list of recipes matching the criteria.</returns>
        public async Task<List<Recipe>> SearchRecipesByModeAndKeywordsAsync(string keywords, string? mode = null)
        {
            var normalizedKeywords = NormalizeKeywords(keywords);
            var cacheKey = $"recipeSearch_{mode?.ToLower() ?? "all"}_{normalizedKeywords}";

            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var keywordList = keywords.Split(',')
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .ToArray();

                var query = _dbContext.Recipes.AsQueryable();
                query = ApplyModeFilter(query, mode);
                query = ApplyKeywordSearch(query, keywordList);

                return await query
                    .Include(r => r.Ingredients)
                    .Include(r => r.Instructions)
                    .Include(r => r.Author)
                    .OrderBy(r => r.RecipeID)
                    .AsSplitQuery()
                    .ToListAsync();
            }, TimeSpan.FromHours(24));
        }

        /// <summary>
        /// Combines recipes from multiple cached recipe types, ensuring uniqueness.
        /// </summary>
        /// <param name="recipeTypes">An array of recipe types (e.g., "Dinner", "Breakfast") to fetch from cache.</param>
        /// <returns>A combined list of unique recipes.</returns>
        public async Task<List<Recipe>> GetCombinedCachedRecipesAsync(params string[] recipeTypes)
        {
            var allRecipes = new List<Recipe>();
            var seenIds = new HashSet<int>();

            foreach (var type in recipeTypes)
            {
                // This method currently relies on a GetSearchRecipesCacheKey that doesn't exist.
                // Assuming it refers to the cache key used by SearchRecipesByIngredientsAsync or similar.
                // For simplicity, directly constructing a similar key here, but ideally, this would be consistent.
                var cacheKey = $"recipeSearchRecipes_{NormalizeKeywords(type)}";
                var cachedData = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedData))
                {
                    var recipes = JsonSerializer.Deserialize<List<Recipe>>(cachedData, _jsonOptions) ?? new List<Recipe>();
                    foreach (var recipe in recipes)
                    {
                        if (seenIds.Add(recipe.RecipeID))
                        {
                            allRecipes.Add(recipe);
                        }
                    }
                }
            }
            return allRecipes;
        }

        /// <summary>
        /// Adds a new recipe to the database and flushes all relevant caches.
        /// </summary>
        /// <param name="recipe">The recipe object to add.</param>
        /// <returns>The ID of the newly added recipe.</returns>
        public async Task<int> AddRecipeAsync(Recipe recipe)
        {
            _dbContext.Recipes.Add(recipe);
            await _dbContext.SaveChangesAsync();

            await ClearAllCachesAsync(); // Flush all relevant caches

            return recipe.RecipeID;
        }

        /// <summary>
        /// Adds ingredients to a recipe and flushes all relevant caches.
        /// </summary>
        /// <param name="recipeId">The ID of the recipe.</param>
        /// <param name="ingredients">A list of ingredient names.</param>
        public async Task AddRecipeIngredientsAsync(int recipeId, List<string> ingredients)
        {
            var recipeIngredients = ingredients.Select((ingredient, index) => new RecipeIngredients
            {
                RecipeID = recipeId,
                IngredientName = ingredient,
                Quantity = null, // Enhance to parse quantity from ingredient string if needed
                Unit = null
            }).ToList();

            _dbContext.RecipeIngredients.AddRange(recipeIngredients);
            await _dbContext.SaveChangesAsync();

            await ClearAllCachesAsync(); // Flush all relevant caches
        }

        /// <summary>
        /// Adds instructions to a recipe and flushes all relevant caches.
        /// </summary>
        /// <param name="recipeId">The ID of the recipe.</param>
        /// <param name="instructions">A list of instruction strings.</param>
        public async Task AddRecipeInstructionsAsync(int recipeId, List<string> instructions)
        {
            var recipeInstructions = instructions.Select((instruction, index) => new RecipeInstructions
            {
                RecipeID = recipeId,
                StepNumber = index + 1,
                Instruction = instruction
            }).ToList();

            _dbContext.RecipeInstructions.AddRange(recipeInstructions);
            await _dbContext.SaveChangesAsync();

            await ClearAllCachesAsync(); // Flush all relevant caches
        }

        /// <summary>
        /// Adds nutrition facts to a recipe and flushes all relevant caches.
        /// </summary>
        /// <param name="recipeId">The ID of the recipe.</param>
        /// <param name="calories">The calorie count.</param>
        /// <param name="protein">The protein amount.</param>
        /// <param name="carbs">The carbohydrate amount.</param>
        /// <param name="fat">The fat amount.</param>
        public async Task AddRecipeNutritionAsync(int recipeId, int? calories, int? protein, int? carbs, int? fat)
        {
            var nutrition = new RecipeNutritionFacts
            {
                RecipeID = recipeId,
                Calories = calories?.ToString(),
                Protein = protein?.ToString(),
                Carbohydrates = carbs?.ToString(),
                Fat = fat?.ToString()
            };

            _dbContext.RecipeNutritionFacts.Add(nutrition);
            await _dbContext.SaveChangesAsync();

            await ClearAllCachesAsync(); // Flush all relevant caches
        }

        /// <summary>
        /// Clears all potentially affected cache entries when data changes.
        /// This is a broad flush to ensure data consistency after modifications.
        /// </summary>
        private async Task ClearAllCachesAsync()
        {
            // The DistributedCache does not have a "ClearAll" or "FlushAll" method directly.
            // You typically need to remove keys individually or use a pattern if your cache provider supports it.
            // For a complete flush, you'd iterate through known patterns or prefixes.

            // Example: Remove all recipes cache
            await _cache.RemoveAsync("recipeAllRecipes");

            // You might need a more sophisticated way to manage cache keys for search results.
            // For a simpler approach, after a data modification, it's often easiest to invalidate
            // broad categories of cached data.
            // A more advanced solution would involve tracking cache keys more precisely
            // or using a cache provider with "tagging" or "pattern-based" invalidation.

            // For demonstration, we'll iterate through some common patterns.
            // In a real-world scenario, you might have a mechanism to list all keys or
            // use a more robust cache invalidation strategy.
            // The following loop is an attempt to clear common search caches, but it's not exhaustive.

            // Clear specific recipe details if a specific recipe was modified
            // (This method is called without a recipeId in the refactored Add methods,
            // so we remove the specific recipe ID parameter from here and make it a broader flush.)

            // Example: Clear caches related to common filters and modes.
            // This is a simplified approach, a true "flush all" might require deeper integration
            // with your caching solution or a cache key management system.
            var commonKeywords = new[] { "", "Dinner", "Breakfast", "Lunch", "Snack", "Dessert",
                                       "Main Course", "Appetizer", "Side Dish", "Soup", "Salad",
                                       "Healthy", "Vegetarian", "Vegan", "Comfort Food" };
            var modes = new[] { null, "user", "cookbook" }; // Include null for cases without a mode

            foreach (var mode in modes)
            {
                foreach (var keyword in commonKeywords)
                {
                    var cacheKey = $"recipeSearch_{mode?.ToLower() ?? "all"}_{NormalizeKeywords(keyword)}";
                    await _cache.RemoveAsync(cacheKey);
                }
            }

            // Also clear individual recipe detail caches if you want to ensure they are fresh on next access.
            // This would typically involve iterating through all known recipe IDs, which might not be practical.
            // A common approach is to set a shorter expiration for individual item caches, or rely on a "stale while revalidate" pattern.
            // For a complete "flush all," some cache providers offer explicit commands, but IDistributedCache does not.
            // If you need a full flush, consider a cache-specific command if available (e.g., Redis FLUSHDB).
        }

        /// <summary>
        /// Prepopulates the cache for all recipes and common recipe type filters.
        /// </summary>
        public async Task PrepopulateCacheAsync()
        {
            // Prepopulate all recipes
            await GetAllRecipesAsync();

            // Prepopulate common search filters
            var filters = new[] { "Dinner", "Breakfast", "Lunch", "Snack", "Dessert",
                "Main Course", "Appetizer", "Side Dish", "Soup", "Salad","Healthy","Vegetarian","Vegan","Comfort Food"};

            foreach (var filter in filters)
            {
                await SearchRecipesByIngredientsAsync(filter);
            }
        }
    }
}