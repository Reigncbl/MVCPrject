using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MVCPrject.Models;
using LinqKit; // Ensure you have LinqKit installed: dotnet add package LinqKit.Microsoft.EntityFrameworkCore

namespace MVCPrject.Data
{
    public class RecipeManipulationService
    {
        private readonly DBContext _dbContext;
        private readonly IDistributedCache _cache;
        private readonly JsonSerializerOptions _jsonOptions;

        // Static readonly arrays for common filters and modes to avoid recreating them
        // and to centralize these definitions.
        private static readonly string[] CommonFilters = { "Dinner", "Breakfast", "Lunch", "Snack", "Dessert",
            "Main Course", "Appetizer", "Side Dish", "Soup", "Salad", "Healthy", "Vegetarian", "Vegan", "Comfort Food" };
        private static readonly string[] Modes = { "user", "cookbook" }; // "all" is handled by null mode

        // Cache expiration times (can be configured externally if needed)
        private static readonly TimeSpan DefaultRecipeCacheExpiration = TimeSpan.FromHours(10);
        private static readonly TimeSpan SearchRecipeCacheExpiration = TimeSpan.FromHours(24); // Longer for search results

        public RecipeManipulationService(DBContext dbContext, IDistributedCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
            _jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                // Add other serialization options if needed, e.g., PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
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
        private const int MaxCacheSizeBytes = 256 * 1024; // 256 KB
        // Only cache recipes that are published (example business rule)
        private IEnumerable<Recipe> FilterCacheableRecipes(IEnumerable<Recipe> recipes)
        {
            // Example: Only cache recipes that are published (add your own logic as needed)
            return recipes.Where(r => r.GetType().GetProperty("IsPublished") == null ||
                                     (bool?)r.GetType().GetProperty("IsPublished")?.GetValue(r) != false);
        }

        private bool ShouldCache<T>(T data)
        {
            if (data == null) return false;
            if (data is System.Collections.ICollection col && col.Count == 0) return false;
            var serialized = JsonSerializer.Serialize(data, _jsonOptions);
            if (System.Text.Encoding.UTF8.GetByteCount(serialized) > MaxCacheSizeBytes) return false;
            return true;
        }

        private async Task<T?> GetOrSetCacheAsync<T>(string cacheKey, Func<Task<T?>> factory, TimeSpan expiration) where T : class
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    return JsonSerializer.Deserialize<T?>(cachedData, _jsonOptions);
                }

                var data = await factory();
                if (ShouldCache(data))
                {
                    var serializedData = JsonSerializer.Serialize(data, _jsonOptions);
                    await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expiration
                    });
                }
                return data;
            }
            catch (StackExchange.Redis.RedisTimeoutException ex)
            {
                // Log the timeout exception
                Console.WriteLine($"Redis Timeout: {ex.Message}");
                // Fallback: If cache fails, fetch directly from the database without caching
                return await factory();
            }
            catch (Exception ex)
            {
                // Log other caching-related exceptions
                Console.WriteLine($"Caching error for key {cacheKey}: {ex.Message}");
                // Fallback: If cache fails, fetch directly from the database without caching
                return await factory();
            }
        }

        /// <summary>
        /// Normalizes keywords by trimming, converting to lowercase, and joining them with commas in sorted order.
        /// This ensures consistent cache keys regardless of input order or casing.
        /// </summary>
        /// <param name="keywords">The comma-separated string of keywords.</param>
        /// <returns>A normalized string of keywords.</returns>
        private string NormalizeKeywords(string keywords)
        {
            // Handle empty or null keywords for consistent behavior
            if (string.IsNullOrWhiteSpace(keywords)) return string.Empty;

            return string.Join(",", keywords.Split(',')
                .Select(k => k.Trim().ToLowerInvariant())
                .Where(k => !string.IsNullOrEmpty(k))
                .OrderBy(k => k));
        }

        /// <summary>
        /// Applies keyword search to a queryable collection of recipes.
        /// Searches in Ingredients, RecipeName, Author Name, or RecipeType.
        /// </summary>
        /// <param name="query">The IQueryable of recipes.</param>
        /// <param name="keywords">An array of keywords to search for.</param>
        /// <returns>The filtered IQueryable of recipes.</returns>
        private IQueryable<Recipe> ApplyKeywordSearch(IQueryable<Recipe> query, string[] keywords)
        {
            if (!keywords.Any()) return query;

            var predicate = PredicateBuilder.New<Recipe>(true); // Start with a true predicate for ORing
            foreach (var keyword in keywords)
            {
                var k = keyword; // Capture keyword for closure in LINQ expression
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
        /// "user" recipes have RecipeMode = "user". "cookbook" recipes have RecipeMode = null or "cookbook".
        /// </summary>
        /// <param name="query">The IQueryable of recipes.</param>
        /// <param name="mode">The mode to filter by ("user" or "cookbook").</param>
        /// <returns>The filtered IQueryable of recipes.</returns>
        private string[] ParseKeywords(string keywords)
        {
            if (string.IsNullOrWhiteSpace(keywords)) return Array.Empty<string>();
            return keywords.Split(',')
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrEmpty(k))
                .ToArray();
        }

        private string GetSearchCacheKey(string keywords, string? mode)
        {
            var normalizedKeywords = NormalizeKeywords(keywords);
            var normalizedMode = mode?.ToLower() ?? "all";
            return $"recipeSearch_{normalizedMode}_{normalizedKeywords}";
        }

        private IQueryable<Recipe> ApplyModeFilter(IQueryable<Recipe> query, string? mode)
        {
            if (string.IsNullOrWhiteSpace(mode)) return query;

            var lowerMode = mode.ToLower();
            return lowerMode switch
            {
                "user" => query.Where(r => r.RecipeMode != null && r.RecipeMode.ToLower() == "user"),
                "cookbook" => query.Where(r => r.RecipeMode == null || r.RecipeMode.ToLower() == "cookbook"),
                _ => query
            };
        }

        /// <summary>
        /// Fetches a single recipe's details with nutrition facts, utilizing caching for the core recipe data.
        /// Nutrition facts are always fetched fresh from the database (not cached with the recipe).
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
            }, DefaultRecipeCacheExpiration);

            if (recipe == null) return null;

            // Nutrition facts are often dynamic or frequently updated, so fetching fresh is safer.
            // If they are static and performance critical, they could also be cached.
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
        /// For large datasets, consider returning a paginated result or a summary DTO.
        /// </summary>
        /// <returns>A list of all recipes.</returns>
        public async Task<List<Recipe>> GetAllRecipesAsync()
        {
            var recipes = await GetOrSetCacheAsync("recipeAllRecipes", async () =>
            {
                var dbRecipes = await _dbContext.Recipes
                    .Include(r => r.Author)
                    .OrderBy(r => r.RecipeID)
                    .ToListAsync();
                return FilterCacheableRecipes(dbRecipes).ToList();
            }, DefaultRecipeCacheExpiration);
            return recipes ?? new List<Recipe>();
        }

        /// <summary>
        /// Searches for recipes by keywords (ingredients, recipe name, author, type), utilizing caching.
        /// </summary>
        /// <param name="keywords">Comma-separated keywords to search for.</param>
        /// <returns>A list of recipes matching the keywords.</returns>
        private Task<List<Recipe>> ExecuteRecipeSearchQueryAsync(IQueryable<Recipe> query)
        {
            return query
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .Include(r => r.Author)
                .OrderBy(r => r.RecipeID)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<List<Recipe>> SearchRecipesByIngredientsAsync(string keywords)
        {
            return await SearchRecipesByModeAndKeywordsAsync(keywords, null);
        }

        public async Task<List<Recipe>> SearchRecipesByModeAndKeywordsAsync(string keywords, string? mode = null)
        {
            var cacheKey = GetSearchCacheKey(keywords, mode);
            var recipes = await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var keywordList = ParseKeywords(keywords);
                var query = _dbContext.Recipes.AsQueryable();
                query = ApplyModeFilter(query, mode);
                query = ApplyKeywordSearch(query, keywordList);
                var dbRecipes = await ExecuteRecipeSearchQueryAsync(query);
                return FilterCacheableRecipes(dbRecipes).ToList();
            }, SearchRecipeCacheExpiration);
            return recipes ?? new List<Recipe>();
        }

        /// <summary>
        /// Combines recipes from multiple cached recipe types/searches, ensuring uniqueness.
        /// This method assumes the cache keys for recipe types are generated consistently
        /// with `SearchRecipesByIngredientsAsync`.
        /// </summary>
        /// <param name="recipeTypes">An array of recipe types (e.g., "Dinner", "Breakfast") to fetch from cache.</param>
        /// <returns>A combined list of unique recipes.</returns>
        public async Task<List<Recipe>> GetCombinedCachedRecipesAsync(params string[] recipeTypes)
        {
            var allRecipes = new List<Recipe>();
            var seenIds = new HashSet<int>(); // To ensure uniqueness of recipes

            foreach (var type in recipeTypes)
            {
                var normalizedType = NormalizeKeywords(type); // Normalize to match cache key format
                var cacheKey = $"recipeSearchByIngredients_{normalizedType}"; // Assuming this is how it was cached

                // Use GetFromCacheAsync helper to retrieve data
                var recipes = await GetFromCacheAsync<List<Recipe>>(cacheKey) ?? new List<Recipe>();

                foreach (var recipe in recipes)
                {
                    if (seenIds.Add(recipe.RecipeID)) // Add only if not already seen
                    {
                        allRecipes.Add(recipe);
                    }
                }
            }
            return allRecipes;
        }

        /// <summary>
        /// Generic helper to get data from cache.
        /// </summary>
        private async Task<T?> GetFromCacheAsync<T>(string cacheKey) where T : class
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey);
                return string.IsNullOrEmpty(cachedData) ? null : JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
            }
            catch (StackExchange.Redis.RedisTimeoutException ex)
            {
                Console.WriteLine($"Redis Timeout during GetFromCacheAsync for key {cacheKey}: {ex.Message}");
                return null; // Return null, allowing fallback to database
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving from cache for key {cacheKey}: {ex.Message}");
                return null; // Return null, allowing fallback to database
            }
        }

        /// <summary>
        /// Adds a new recipe to the database and flushes all relevant caches to ensure data consistency.
        /// </summary>
        /// <param name="recipe">The recipe object to add.</param>
        /// <returns>The ID of the newly added recipe.</returns>
        public async Task<int> AddRecipeAsync(Recipe recipe)
        {
            _dbContext.Recipes.Add(recipe);
            await _dbContext.SaveChangesAsync();

            await ClearAllCachesAsync(); // Flush all potentially affected caches

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
                Quantity = null, // Enhance: Could parse quantity from ingredient string
                Unit = null
            }).ToList();

            _dbContext.RecipeIngredients.AddRange(recipeIngredients);
            await _dbContext.SaveChangesAsync();

            await ClearAllCachesAsync(); // Flush all potentially affected caches
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

            await ClearAllCachesAsync(); // Flush all potentially affected caches
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

            await ClearAllCachesAsync(); // Flush all potentially affected caches
        }

        /// <summary>
        /// Clears all potentially affected cache entries when data changes.
        /// This method broadly invalidates cache entries to ensure consistency after modifications.
        /// For a true "flush all", consider direct Redis commands if using StackExchange.Redis.
        /// </summary>
        private async Task ClearAllCachesAsync()
        {
            // Always clear the "all recipes" cache
            await _cache.RemoveAsync("recipeAllRecipes");

            // Clear cache for specific recipe details if known, though this method is called broadly
            // If you have the specific recipe ID that was modified, you could remove its detail cache:
            // if (modifiedRecipeId.HasValue) await _cache.RemoveAsync($"recipeDetails_{modifiedRecipeId.Value}");
            // However, since it's a new add, we just invalidate broad categories.

            // Clear search caches for common filters and modes.
            // This is an approximation of a "flush all relevant search caches."
            // For a very large number of keywords/combinations, this could be slow.
            // A more robust solution involves Redis keyspace notifications, tags, or a dedicated cache invalidation service.

            // Iterate through common filters and modes to remove their specific search caches
            foreach (var mode in Modes.Append((string?)null)) // Include null for "all" mode
            {
                var normalizedMode = string.IsNullOrEmpty(mode) ? "all" : mode.ToLowerInvariant();

                foreach (var filter in CommonFilters.Append(string.Empty)) // Include empty string for "no filter"
                {
                    var normalizedKeywords = NormalizeKeywords(filter);
                    var cacheKey = $"recipeSearchByIngredients_{normalizedKeywords}"; // From SearchRecipesByIngredientsAsync
                    await _cache.RemoveAsync(cacheKey);

                    cacheKey = $"recipeSearch_{normalizedMode}_{normalizedKeywords}"; // From SearchRecipesByModeAndKeywordsAsync
                    await _cache.RemoveAsync(cacheKey);
                }
            }
        }

        /// <summary>
        /// Prepopulates the cache for frequently accessed data, such as all recipes
        /// and recipes categorized by common filters.
        /// </summary>
        public async Task PrepopulateCacheAsync()
        {
            // Prepopulate all recipes
            await GetAllRecipesAsync();

            // Prepopulate common search filters for both modes (user and cookbook) and no mode
            foreach (var mode in Modes.Append((string?)null))
            {
                foreach (var filter in CommonFilters)
                {
                    await SearchRecipesByModeAndKeywordsAsync(filter, mode);
                }
                // Also prepopulate with empty keywords for each mode
                await SearchRecipesByModeAndKeywordsAsync(string.Empty, mode);
            }
            // Prepopulate ingredient-based searches for common filters
            foreach (var filter in CommonFilters)
            {
                await SearchRecipesByIngredientsAsync(filter);
            }
            // Also prepopulate ingredient-based search with empty keywords
            await SearchRecipesByIngredientsAsync(string.Empty);
        }
    }
}