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
                    .Include(r => r.Author)
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
        public async Task<List<Recipe>> GetAllRecipesAsync()
        {
            string cacheKey = "recipeAllRecipes";

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<List<Recipe>>(cachedData) ?? new List<Recipe>();
            }

            var recipes = await _dbContext.Recipes
                .Include(r => r.Author)
                .OrderBy(r => r.RecipeID)
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
                        (r.Author != null && EF.Functions.Like(r.Author.Name, $"%{k}%")) ||
                        EF.Functions.Like(r.RecipeType, $"%{k}%"));
                }
                query = query.Where(predicate);
            }

            var recipes = await query
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .Include(r => r.Author)
                .OrderBy(r => r.RecipeID)
                .AsSplitQuery()
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

        // Add a new recipe to the database
        public async Task<int> AddRecipeAsync(Recipe recipe)
        {
            _dbContext.Recipes.Add(recipe);
            await _dbContext.SaveChangesAsync();

            // Clear relevant cache entries
            await ClearRecipeCacheAsync();

            return recipe.RecipeID;
        }

        // Add ingredients to a recipe
        public async Task AddRecipeIngredientsAsync(int recipeId, List<string> ingredients)
        {
            var recipeIngredients = ingredients.Select((ingredient, index) => new RecipeIngredients
            {
                RecipeID = recipeId,
                IngredientName = ingredient,
                Quantity = null, // Could be enhanced to parse quantity from ingredient string
                Unit = null
            }).ToList();

            _dbContext.RecipeIngredients.AddRange(recipeIngredients);
            await _dbContext.SaveChangesAsync();
        }

        // Add instructions to a recipe
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
        }

        // Add nutrition facts to a recipe
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
        }

        // Clear recipe cache when data changes
        private async Task ClearRecipeCacheAsync()
        {
            // Clear all recipes cache
            await _cache.RemoveAsync("recipeAllRecipes_10");

            // Clear search caches for common filters
            var filters = new[] { "Dinner", "Breakfast", "Lunch", "Snack", "Dessert",
                "Main Course", "Appetizer", "Side Dish", "Soup", "Salad","Healthy","Vegetarian","Vegan","Comfort Food"};

            foreach (var filter in filters)
            {
                string filterKey = GetSearchRecipesCacheKey(filter);
                await _cache.RemoveAsync(filterKey);
            }
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
        public async Task<List<Recipe>> SearchRecipesByModeAndKeywordsAsync(string keywords, string? mode = null)
        {
            Console.WriteLine($"[DEBUG] SearchRecipesByModeAndKeywordsAsync called with keywords: '{keywords}', mode: '{mode}'");
            
            // Normalize cache key
            string cacheKey = $"recipeSearch_{mode}_{keywords}";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedRecipes = JsonSerializer.Deserialize<List<Recipe>>(cachedData) ?? new List<Recipe>();
                Console.WriteLine($"[DEBUG] Returning {cachedRecipes.Count} recipes from cache");
                return cachedRecipes;
            }

            Console.WriteLine("[DEBUG] No cached data found, querying database");

            // Split and normalize keywords
            var keywordList = keywords.Split(',')
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrEmpty(k))
                .ToArray();

            Console.WriteLine($"[DEBUG] Keywords parsed: [{string.Join(", ", keywordList)}]");

            var query = _dbContext.Recipes.AsQueryable();

            // Filter by source type if provided
            if (!string.IsNullOrEmpty(mode))
            {
                Console.WriteLine($"[DEBUG] Applying mode filter: {mode}");
                if (mode.ToLower() == "user")
                {
                    Console.WriteLine("[DEBUG] Filtering for user recipes (RecipeMode = 'user')");
                    query = query.Where(r => r.RecipeMode != null && r.RecipeMode.ToLower() == "user");
                }
                else if (mode.ToLower() == "cookbook")
                {
                    Console.WriteLine("[DEBUG] Filtering for cookbook recipes (RecipeMode = 'cookbook' or null)");
                    query = query.Where(r => r.RecipeMode == null || r.RecipeMode.ToLower() == "cookbook");
                }
            }
            else
            {
                Console.WriteLine("[DEBUG] No mode filter applied, showing all recipes");
            }

            // Apply keyword search
            if (keywordList.Any())
            {
                Console.WriteLine($"[DEBUG] Applying keyword search for: {string.Join(", ", keywordList)}");
                var predicate = PredicateBuilder.New<Recipe>();
                foreach (var keyword in keywordList)
                {
                    var k = keyword;
                    predicate = predicate.Or(r =>
                        r.Ingredients.Any(i => EF.Functions.Like(i.IngredientName, $"%{k}%")) ||
                        EF.Functions.Like(r.RecipeName, $"%{k}%") ||
                        (r.Author != null && EF.Functions.Like(r.Author.Name, $"%{k}%")) ||
                        EF.Functions.Like(r.RecipeType, $"%{k}%"));
                }
                query = query.Where(predicate);
            }
            else
            {
                Console.WriteLine("[DEBUG] No keyword search applied");
            }

            Console.WriteLine("[DEBUG] Executing database query...");

            // Execute the query
            var recipes = await query
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .Include(r => r.Author)
                .OrderBy(r => r.RecipeID)
                .AsSplitQuery()
                .ToListAsync();

            Console.WriteLine($"[DEBUG] Query returned {recipes.Count} recipes");
            
            // Log some sample recipe modes for debugging
            if (recipes.Any())
            {
                var sampleRecipes = recipes.Take(5);
                foreach (var recipe in sampleRecipes)
                {
                    Console.WriteLine($"[DEBUG] Recipe ID: {recipe.RecipeID}, Name: '{recipe.RecipeName}', RecipeMode: '{recipe.RecipeMode}', AuthorId: '{recipe.AuthorId}'");
                }
            }

            // Cache the result
            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            var serializedData = JsonSerializer.Serialize(recipes, jsonOptions);
            await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });

            Console.WriteLine($"[DEBUG] Cached {recipes.Count} recipes and returning result");
            return recipes;
        }

    }
}