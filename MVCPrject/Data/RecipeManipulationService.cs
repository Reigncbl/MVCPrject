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

        public RecipeManipulationService(DBContext dbContext, IDistributedCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        // Fetch a single recipe with caching
        public async Task<Recipe?> GetRecipeDetailsAsync(int id)
        {
            string cacheKey = $"recipeRecipeDetails_{id}";

            // Check cache first
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<Recipe>(cachedData);
            }

            // Cache miss: Fetch from database
            var recipe = await _dbContext.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .AsSplitQuery()
                .FirstOrDefaultAsync(r => r.RecipeID == id);

            if (recipe != null)
            {
                // Cache the result
                var jsonOptions = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };

                var serializedData = JsonSerializer.Serialize(recipe, jsonOptions);
                await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10) // 10 hours
                });
            }

            return recipe;
        }

        // Fetch all recipes with caching
        public async Task<List<Recipe>> GetAllRecipesAsync(int count = 10)
        {
            string cacheKey = $"recipeAllRecipes_{count}";

            // Check cache first
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<List<Recipe>>(cachedData) ?? new List<Recipe>();
            }

            // Cache miss: Fetch from database
            var recipes = await _dbContext.Recipes
                .OrderBy(r => r.RecipeID)
                .Take(count)
                .ToListAsync();

            // Cache the result
            var serializedData = JsonSerializer.Serialize(recipes);
            await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10) // 10 hours
            });

            return recipes;
        }

        // Search recipes by ingredients with caching
        private string GetSearchRecipesCacheKey(string keywords)
        {
            // Normalize keywords to avoid key mismatches (trim, lower, remove extra spaces)
            var normalized = string.Join(",", keywords.Split(',')
                .Select(k => k.Trim().ToLowerInvariant())
                .Where(k => !string.IsNullOrEmpty(k))
                .OrderBy(k => k));

            // Use the normalized string directly instead of GetHashCode()
            return $"recipeSearchRecipes_{normalized}";
        }

        public async Task<List<Recipe>> SearchRecipesByIngredientsAsync(string keywords)
        {
            string cacheKey = GetSearchRecipesCacheKey(keywords);

            // Check cache first
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<List<Recipe>>(cachedData) ?? new List<Recipe>();
            }

            // Cache miss: Build query and fetch from database
            var keywordList = keywords.Split(',')
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrEmpty(k))
                .ToArray();

            var query = _dbContext.Recipes.AsQueryable();

            if (keywordList.Any())
            {
                var predicate = PredicateBuilder.New<Recipe>();
                foreach (var keyword in keywordList)
                {
                    var k = keyword; // Local variable for closure
                    predicate = predicate.Or(r =>
                        r.Ingredients.Any(i => EF.Functions.Like(i.IngredientName, $"%{k}%")) ||
                        EF.Functions.Like(r.RecipeName, $"%{k}%") ||
                        EF.Functions.Like(r.RecipeAuthor, $"%{k}%") ||
                        EF.Functions.Like(r.RecipeType, $"%{k}%"));
                }
                query = query.Where(predicate);
            }

            var recipes = await query
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .OrderBy(r => r.RecipeID)
                .AsSplitQuery()
                .Take(300)
                .ToListAsync();

            // Cache the result
            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            var serializedData = JsonSerializer.Serialize(recipes, jsonOptions);
            await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // 10 hours
            });

            return recipes;
        }

        private bool IsRecipeTypeKeyword(string keyword)
        {
            var recipeTypes = new[] {
                "Dinner", "Breakfast", "Lunch", "Snack", "Dessert",
                "Main Course", "Appetizer", "Side Dish", "Soup", "Salad","Healthy","Vegetarian","Vegan","Comfort Food"
            };
            return recipeTypes.Contains(keyword, StringComparer.OrdinalIgnoreCase);
        }

        // Prepopulate cache for all recipe type filters
        public async Task PrepopulateCacheAsync()
        {
            // Prepopulate all recipes (default 10) only if not already cached
            string allRecipesKey = $"recipeAllRecipes_10";
            var allRecipesCache = await _cache.GetStringAsync(allRecipesKey);
            if (string.IsNullOrEmpty(allRecipesCache))
            {
                await GetAllRecipesAsync();
            }

            // Prepopulate for each filter only if not already cached
            var filters = new[] { "Dinner", "Breakfast", "Lunch", "Snack", "Dessert",
                "Main Course", "Appetizer", "Side Dish", "Soup", "Salad","Healthy","Vegetarian","Vegan","Comfort Food"};
            foreach (var filter in filters)
            {
                string filterKey = GetSearchRecipesCacheKey(filter);
                var filterCache = await _cache.GetStringAsync(filterKey);
                if (string.IsNullOrEmpty(filterCache))
                {
                    await SearchRecipesByIngredientsAsync(filter);
                }
            }
        }

    }
}