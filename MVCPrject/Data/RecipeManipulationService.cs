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

        // Fetch a single recipe with caching (recipe only, nutrition fetched separately)
        public async Task<RecipeDetailsViewModel?> GetRecipeDetailsWithNutritionAsync(int id)
        {
            string cacheKey = $"recipeDetails_{id}";

            // Check the cache for recipe data only
            var cachedData = await _cache.GetStringAsync(cacheKey);
            Recipe? recipe = null;

            if (!string.IsNullOrEmpty(cachedData))
            {
                recipe = JsonSerializer.Deserialize<Recipe>(cachedData);
            }
            else
            {
                // Fetch recipe from database
                recipe = await _dbContext.Recipes
                    .Include(r => r.Ingredients)
                    .Include(r => r.Instructions)
                    .FirstOrDefaultAsync(r => r.RecipeID == id);

                if (recipe == null)
                    return null;

                // Cache only the recipe data
                var jsonOptions = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };
                var serializedData = JsonSerializer.Serialize(recipe, jsonOptions);

                await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10)
                });
            }

            // Always fetch nutrition facts fresh from database (not cached)
            var nutritionFacts = await _dbContext.RecipeNutritionFacts
                .FirstOrDefaultAsync(nf => nf.RecipeID == id);

#pragma warning disable CS8601 // Possible null reference assignment.
            var viewModel = new RecipeDetailsViewModel
            {
                Recipe = recipe,
                NutritionFacts = nutritionFacts
            };
#pragma warning restore CS8601 // Possible null reference assignment.

            return viewModel;
        }


        // Fetch all recipes with caching
        public async Task<List<Recipe>> GetAllRecipesAsync(int count = 10)
        {
            string cacheKey = $"recipeAllRecipes_{count}";

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<List<Recipe>>(cachedData) ?? new List<Recipe>();
            }

            var recipes = await _dbContext.Recipes
                .OrderBy(r => r.RecipeID)
                .Take(count)
                .ToListAsync();

            var serializedData = JsonSerializer.Serialize(recipes);
            await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10)
            });

            return recipes;
        }

        private string GetSearchRecipesCacheKey(string keywords)
        {
            var normalized = string.Join(",", keywords.Split(',')
                .Select(k => k.Trim().ToLowerInvariant())
                .Where(k => !string.IsNullOrEmpty(k))
                .OrderBy(k => k));

            return $"recipeSearchRecipes_{normalized}";
        }

        public async Task<List<Recipe>> SearchRecipesByIngredientsAsync(string keywords)
        {
            string cacheKey = GetSearchRecipesCacheKey(keywords);

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<List<Recipe>>(cachedData) ?? new List<Recipe>();
            }

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
                    var k = keyword;
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

            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            var serializedData = JsonSerializer.Serialize(recipes, jsonOptions);
            await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });

            return recipes;
        }

        // Combine multiple cached recipe types
        public async Task<List<Recipe>> GetCombinedCachedRecipesAsync(params string[] recipeTypes)
        {
            var allRecipes = new List<Recipe>();
            var seenIds = new HashSet<int>();

            foreach (var type in recipeTypes)
            {
                var cacheKey = GetSearchRecipesCacheKey(type.Trim());
                var cachedData = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedData))
                {
                    var recipes = JsonSerializer.Deserialize<List<Recipe>>(cachedData) ?? new List<Recipe>();
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

        // Prepopulate cache for all recipe type filters
        public async Task PrepopulateCacheAsync()
        {
            string allRecipesKey = $"recipeAllRecipes_10";
            var allRecipesCache = await _cache.GetStringAsync(allRecipesKey);
            if (string.IsNullOrEmpty(allRecipesCache))
            {
                await GetAllRecipesAsync();
            }

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