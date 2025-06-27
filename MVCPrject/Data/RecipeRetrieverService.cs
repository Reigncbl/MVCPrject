using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MVCPrject.Data;
using MVCPrject.Models;
namespace MVCPrject;

using static System.Net.WebRequestMethods;

public class RecipeRetrieverService
{
    private readonly DBContext _dbContext;
    private readonly HttpClient _httpClient = new();

    public RecipeRetrieverService(DBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ScrapeAndSaveUrlsAsync(string category)
    {
        string baseUrl = $"https://panlasangpinoy.com/categories/recipes/{category}/";

        for (int page = 1; page <= 10; page++)
        {
            string url = $"{baseUrl}page/{page}/";
            try
            {
                var html = await _httpClient.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var newLinks = doc.DocumentNode.SelectNodes("//h2[@class='entry-title']/a")
                    ?.Select(n => n.GetAttributeValue("href", "").Trim())
                    .Where(link => !string.IsNullOrEmpty(link) && link.StartsWith("https://"))
                    .Distinct()
                    .ToList();

                if (newLinks == null || !newLinks.Any()) break;

                var existingLinks = await _dbContext.Recipes
          .Where(r => newLinks.Where(link => link != null).Contains(r.RecipeURL))
          .Select(r => r.RecipeURL)
          .ToListAsync();

                var linksToAdd = newLinks.Except(existingLinks)
                                         .Select(link => new Recipe { RecipeURL = link });

                _dbContext.Recipes.AddRange(linksToAdd);
                await _dbContext.SaveChangesAsync();
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error on page {page} for category '{category}': {ex.Message}");
                break;
            }
        }
        Console.WriteLine($"Done scraping category: {category}");
    }

    public async Task ScrapeAllRecipesAsync()
    {
        var categories = new[] { "chicken-recipes",
                  "pork-recipes",
                  "dessert-and-pastry-recipes",
                  "beef-recipes",
                  "vegetable-recipes",
                  "fish-recipes-recipes",
                  "pasta-recipes",
                  "rice-recipes",
                  "eggs",
                  "tofu-recipes-recipes",
                  "noodle-recipes"};
        foreach (var category in categories)
        {
            await ScrapeAndSaveUrlsAsync(category);
        }
        Console.WriteLine("All categories scraped successfully.");
    }


    public async Task ScrapeAndUpdateRecipesAsync()
    {
        var recipes = await _dbContext.Recipes.ToListAsync();

        foreach (var recipe in recipes)
        {
            if (!string.IsNullOrEmpty(recipe.RecipeURL))
            {
                Console.WriteLine($"Scraping: {recipe.RecipeURL}");

                try
                {
                    await RetrieveAndUpdateRecipeAsync(recipe);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scraping {recipe.RecipeURL}: {ex.Message}");
                }

                await Task.Delay(1000);
            }
        }

        await _dbContext.SaveChangesAsync();
        Console.WriteLine("Scraping completed!");
    }

    private async Task RetrieveAndUpdateRecipeAsync(Recipe recipe)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        var html = await client.GetStringAsync(recipe.RecipeURL);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Extract data directly
        var recipeName = CleanText(GetNodeText(doc, "//h1[contains(@class, 'entry-title')]"));
        var description = CleanText(GetNodeText(doc, "(//p)[5]"));
        var image = GetImageSrc(doc, "https://panlasangpinoy.com/wp-content/uploads/");
        var prepTime = ExtractTimeInMinutes(GetNodeText(doc, "//span[contains(@class, 'wprm-recipe-prep_time')]"));
        var cookTime = ExtractTimeInMinutes(GetNodeText(doc, "//span[contains(@class, 'wprm-recipe-cook_time')]"));
        var servings = ExtractServings(doc);
        var ingredients = ExtractIngredients(doc);
        var instructions = ExtractInstructions(doc);

        // Update recipe properties
        if (!string.IsNullOrEmpty(recipeName))
            recipe.RecipeName = recipeName;

        if (!string.IsNullOrEmpty(description))
            recipe.RecipeDescription = description;

        if (!string.IsNullOrEmpty(image))
            recipe.RecipeImage = image;

        if (prepTime.HasValue)
            recipe.PrepTimeMin = prepTime.Value;

        if (cookTime.HasValue)
            recipe.CookTimeMin = cookTime.Value;

        if (!string.IsNullOrEmpty(servings))
            recipe.RecipeServings = servings;

        // Clear existing data
        var existingIngredients = await _dbContext.Set<RecipeIngredients>()
            .Where(ri => ri.RecipeID == recipe.RecipeID)
            .ToListAsync();
        _dbContext.Set<RecipeIngredients>().RemoveRange(existingIngredients);

        var existingInstructions = await _dbContext.Set<RecipeInstructions>()
            .Where(ri => ri.RecipeID == recipe.RecipeID)
            .ToListAsync();
        _dbContext.Set<RecipeInstructions>().RemoveRange(existingInstructions);

        // Add new ingredients
        foreach (var rawIngredient in ingredients)
        {
            var (quantity, unit, name) = ParseIngredient(rawIngredient);

            if (string.IsNullOrWhiteSpace(name)) continue;

            var ingredient = new RecipeIngredients
            {
                RecipeID = recipe.RecipeID,
                IngredientName = name,
                Quantity = quantity,
                Unit = unit
            };

            _dbContext.Set<RecipeIngredients>().Add(ingredient);
        }

        // Add new instructions
        for (int i = 0; i < instructions.Count; i++)
        {
            var instruction = new RecipeInstructions
            {
                RecipeID = recipe.RecipeID,
                StepNumber = i + 1,
                Instruction = instructions[i]
            };

            _dbContext.Set<RecipeInstructions>().Add(instruction);
        }
    }

    // Helper methods (same as before)
    private static string GetNodeText(HtmlDocument doc, string xpath) =>
        doc.DocumentNode.SelectSingleNode(xpath)?.InnerText?.Trim() ?? "";

    private static string GetImageSrc(HtmlDocument doc, string startsWith) =>
        doc.DocumentNode.Descendants("img")
            .FirstOrDefault(img => img.GetAttributeValue("src", "").StartsWith(startsWith))
            ?.GetAttributeValue("src", "") ?? "";

    private static string ExtractServings(HtmlDocument doc)
    {
        var servingsInput = doc.DocumentNode.SelectSingleNode("//input[contains(@class, 'wprm-recipe-servings')]");
        if (servingsInput != null)
        {
            var value = servingsInput.GetAttributeValue("value", "");
            return CleanServings(value);
        }

        var servingsSpan = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'wprm-recipe-servings')]");
        if (servingsSpan != null)
        {
            return CleanServings(servingsSpan.InnerText?.Trim() ?? "");
        }

        return "";
    }

    private static string CleanServings(string servings)
    {
        if (string.IsNullOrEmpty(servings)) return "";

        servings = CleanText(servings);
        var numberMatch = Regex.Match(servings, @"(\d+)");
        if (numberMatch.Success)
        {
            return numberMatch.Groups[1].Value;
        }

        return servings;
    }

    private static List<string> ExtractIngredients(HtmlDocument doc)
    {
        return doc.DocumentNode.SelectNodes("//li[@class='wprm-recipe-ingredient']")
            ?.Select(node =>
            {
                var amount = node.SelectSingleNode("./span[@class='wprm-recipe-ingredient-amount']")?.InnerText?.Trim() ?? "";
                var unit = node.SelectSingleNode("./span[@class='wprm-recipe-ingredient-unit']")?.InnerText?.Trim() ?? "";
                var name = node.SelectSingleNode("./span[@class='wprm-recipe-ingredient-name']")?.InnerText?.Trim() ?? "";

                return $"{amount} {unit} {name}".Trim();
            })
            .Where(ingredient => !string.IsNullOrWhiteSpace(ingredient))
            .ToList() ?? new List<string>();
    }

    private static List<string> ExtractInstructions(HtmlDocument doc)
    {
        return doc.DocumentNode.SelectNodes("//div[contains(@class, 'wprm-recipe-instruction-group')]/ul/li/div[contains(@class, 'wprm-recipe-instruction-text')]")
            ?.Select(node => CleanText(node.InnerText))
            .Where(instruction => !string.IsNullOrWhiteSpace(instruction))
            .ToList() ?? new List<string>();
    }

    // Returns tuple instead of helper class
    private static (decimal? quantity, string? unit, string name) ParseIngredient(string rawIngredient)
    {
        if (string.IsNullOrEmpty(rawIngredient))
            return (null, null, "");

        rawIngredient = CleanText(rawIngredient);

        // Handle mixed numbers like "2 1/2"
        var mixedNumberMatch = Regex.Match(rawIngredient, @"^(\d+)\s+(\d+/\d+)\s*([a-zA-Z]*)\s*(.+)$");
        if (mixedNumberMatch.Success)
        {
            var wholeNumber = decimal.Parse(mixedNumberMatch.Groups[1].Value);
            var fractionParts = mixedNumberMatch.Groups[2].Value.Split('/');
            var fraction = decimal.Parse(fractionParts[0]) / decimal.Parse(fractionParts[1]);
            var unit = mixedNumberMatch.Groups[3].Value.Trim();
            var name = mixedNumberMatch.Groups[4].Value.Trim();

            return (wholeNumber + fraction, string.IsNullOrEmpty(unit) ? null : unit, name);
        }

        // Handle simple patterns
        var match = Regex.Match(rawIngredient, @"^(\d+(?:\.\d+)?(?:/\d+)?)\s*([a-zA-Z]+)?\s*(.+)$");

        if (match.Success)
        {
            var quantityStr = match.Groups[1].Value;
            var unit = match.Groups[2].Value.Trim();
            var name = match.Groups[3].Value.Trim();

            decimal? quantity = null;

            if (quantityStr.Contains('/'))
            {
                var parts = quantityStr.Split('/');
                if (parts.Length == 2 &&
                    decimal.TryParse(parts[0], out var numerator) &&
                    decimal.TryParse(parts[1], out var denominator) &&
                    denominator != 0)
                {
                    quantity = numerator / denominator;
                }
            }
            else if (decimal.TryParse(quantityStr, out var parsedQuantity))
            {
                quantity = parsedQuantity;
            }

            return (quantity, string.IsNullOrEmpty(unit) ? null : unit, name);
        }

        // Try simple number pattern
        var simpleMatch = Regex.Match(rawIngredient, @"^(\d+(?:\.\d+)?)\s+(.+)$");
        if (simpleMatch.Success && decimal.TryParse(simpleMatch.Groups[1].Value, out var qty))
        {
            return (qty, null, simpleMatch.Groups[2].Value.Trim());
        }

        return (null, null, rawIngredient);
    }

    private static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        text = System.Net.WebUtility.HtmlDecode(text);
        text = text.Replace("&#8217;", "'")
                  .Replace("&#8216;", "'")
                  .Replace("&#8220;", "\"")
                  .Replace("&#8221;", "\"")
                  .Replace("&nbsp;", " ");

        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    private static int? ExtractTimeInMinutes(string timeString)
    {
        if (string.IsNullOrEmpty(timeString)) return null;

        timeString = timeString.ToLower();

        var hourMatch = Regex.Match(timeString, @"(\d+)\s*(?:hour|hr|h)");
        var minuteMatch = Regex.Match(timeString, @"(\d+)\s*(?:minute|min|m)");

        int totalMinutes = 0;

        if (hourMatch.Success)
            totalMinutes += int.Parse(hourMatch.Groups[1].Value) * 60;

        if (minuteMatch.Success)
            totalMinutes += int.Parse(minuteMatch.Groups[1].Value);

        if (totalMinutes == 0)
        {
            var numberMatch = Regex.Match(timeString, @"(\d+)");
            if (numberMatch.Success)
                totalMinutes = int.Parse(numberMatch.Groups[1].Value);
        }

        return totalMinutes > 0 ? totalMinutes : null;
    }

    public async Task AutomateScrapingAndUpdatingRecipes()
    {
        Console.WriteLine("Starting automation of scraping and updating recipes...");

        try
        {
            // Step 1: Scrape all categories to fetch URLs
            Console.WriteLine("Step 1: Scraping URLs from all categories...");
            await ScrapeAllRecipesAsync();
            Console.WriteLine("URL scraping completed!");

            // Step 2: Update each recipe in the database with detailed information
            Console.WriteLine("Step 2: Updating recipes with detailed information...");
            await ScrapeAndUpdateRecipesAsync();
            Console.WriteLine("Recipe updates completed!");

            // Step 3: Automation complete
            Console.WriteLine("Automation process completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during automation: {ex.Message}");
        }
    }
    public async Task ScrapeAndSaveRecipeNutritionAsync(int recipeId, string recipeUrl)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(recipeUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Locate the nutrition container
            var nutritionContainer = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'wprm-nutrition-label-container')]");
            if (nutritionContainer == null)
            {
                Console.WriteLine($"No nutrition information found for RecipeID {recipeId}");
                return;
            }

            // Map DB fields to label text
            var nutritionMap = new Dictionary<string, string>
            {
                {"Calories", "Calories"},
                {"Carbohydrates", "Carbohydrates"},
                {"Protein", "Protein"},
                {"Fat", "Fat"},
                {"Monounsaturated_Fat", "Monounsaturated Fat"},
                {"Trans_Fat", "Trans Fat"},
                {"Cholesterol", "Cholesterol"},
                {"Sodium", "Sodium"},
                {"Potassium", "Potassium"},
                {"Fiber", "Fiber"},
                {"Sugar", "Sugar"},
                {"Vitamin_A", "Vitamin A"},
                {"Vitamin_C", "Vitamin C"},
                {"Calcium", "Calcium"},
                {"Iron", "Iron"}
            };

            // Check if nutrition facts already exist for this recipe
            var nutritionFacts = await _dbContext.RecipeNutritionFacts.FirstOrDefaultAsync(n => n.RecipeID == recipeId);
            bool isNew = false;
            if (nutritionFacts == null)
            {
                nutritionFacts = new RecipeNutritionFacts { RecipeID = recipeId };
                isNew = true;
            }
            var nutritionFactsType = typeof(RecipeNutritionFacts);

            foreach (var kvp in nutritionMap)
            {
                var value = ExtractNutritionValue(nutritionContainer, kvp.Value);
                var prop = nutritionFactsType.GetProperty(kvp.Key);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(nutritionFacts, value);
                }
            }

            // Save to database (add or update)
            if (isNew)
                _dbContext.RecipeNutritionFacts.Add(nutritionFacts);
            else
                _dbContext.RecipeNutritionFacts.Update(nutritionFacts);
            await _dbContext.SaveChangesAsync();

            Console.WriteLine($"Nutrition facts saved for RecipeID {recipeId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error scraping nutrition facts for RecipeID {recipeId}: {ex.Message}");
        }
    }

    // Helper function to extract specific nutrition values
    private string? ExtractNutritionValue(HtmlNode container, string label)
    {
        // Find the span with the label text (e.g., "Calories:")
        var labelSpan = container.SelectSingleNode($".//span[contains(@class, 'wprm-nutrition-label-text-nutrition-label') and normalize-space(text())='{label}:']");
        if (labelSpan != null && labelSpan.ParentNode != null)
        {
            // Find the value span within the same parent
            var valueSpan = labelSpan.ParentNode.SelectSingleNode(".//span[contains(@class, 'wprm-nutrition-label-text-nutrition-value')]");
            return valueSpan?.InnerText?.Trim();
        }
        return null;
    }

    public async Task ScrapeAndSaveNutritionForAllRecipesAsync()
    {
        try
        {
            // Fetch all recipes with non-empty URLs
            var recipes = await _dbContext.Recipes
                .Where(r => !string.IsNullOrEmpty(r.RecipeURL))
                .ToListAsync();

            foreach (var recipe in recipes)
            {
            if (!string.IsNullOrEmpty(recipe.RecipeURL))
            {
            await ScrapeAndSaveRecipeNutritionAsync(recipe.RecipeID, recipe.RecipeURL);
            }
            }

            Console.WriteLine("Nutrition facts scraping completed for all recipes.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in scraping nutrition for all recipes: {ex.Message}");
        }
    }

}



public static class RecipeLabelingPromptBuilder
{
    public static string BuildLabelingPrompt(Recipe recipe)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("You are a culinary expert. Analyze this recipe and provide ONE category label.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Choose from these categories ONLY:");
        promptBuilder.AppendLine("Breakfast, Lunch, Dinner, Appetizer, Dessert, Snack, Beverage, Salad, Soup, Main Course, Side Dish, Vegetarian, Vegan, Healthy, Comfort Food, Quick & Easy");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Recipe Name: {recipe.RecipeName}");

        if (!string.IsNullOrEmpty(recipe.RecipeDescription))
        {
            promptBuilder.AppendLine($"Description: {recipe.RecipeDescription.Substring(0, Math.Min(200, recipe.RecipeDescription.Length))}...");
        }

        if (recipe.Ingredients?.Any() == true)
        {
            var topIngredients = recipe.Ingredients
                .Take(5)
                .Select(i => i.IngredientName)
                .Where(name => !string.IsNullOrEmpty(name));

            if (topIngredients.Any())
            {
                promptBuilder.AppendLine($"Key Ingredients: {string.Join(", ", topIngredients)}");
            }
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Return ONLY the single most appropriate category name from the list above. No explanation, no punctuation, just the category name.");

        return promptBuilder.ToString();
    }


}

public class RecipeLabelingService
{
    private readonly DBContext _dbContext;
    private readonly Kernel _kernel;
    private readonly ILogger<RecipeLabelingService> _logger;
    private readonly int _apiDelayMs;

    public RecipeLabelingService(DBContext dbContext, Kernel kernel, ILogger<RecipeLabelingService> logger)
    {
        _dbContext = dbContext;
        _kernel = kernel;
        _logger = logger;

        _apiDelayMs = 2000;
    }


    public RecipeLabelingService(DBContext dbContext, Kernel kernel, ILogger<RecipeLabelingService> logger, int apiDelayMs)
        : this(dbContext, kernel, logger)
    {
        _apiDelayMs = apiDelayMs; // Allows overriding the default delay
    }

    public async Task LabelAllRecipesAsync()
    {
        _logger.LogInformation("📊 Fetching recipes without labels...");

        var unlabeledRecipes = await _dbContext.Recipes
            .Include(r => r.Ingredients)
            .Where(r => string.IsNullOrEmpty(r.RecipeType))
            .ToListAsync();

        _logger.LogInformation($"Found {unlabeledRecipes.Count} recipes to label.");

        int processed = 0;
        int successful = 0;

        foreach (var recipe in unlabeledRecipes)
        {
            processed++;
            _logger.LogInformation($"[{processed}/{unlabeledRecipes.Count}] Processing: {recipe.RecipeName}...");

            try
            {
                var label = await GenerateRecipeLabelAsync(recipe);
                recipe.RecipeType = label;
                successful++;

                _logger.LogInformation($"✅ Labeled as: {label}");

                await _dbContext.SaveChangesAsync();

                await Task.Delay(_apiDelayMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error labeling recipe '{recipe.RecipeName}' (ID: {recipe.RecipeID}): {ex.Message}");
                recipe.RecipeType = "Uncategorized";
                await _dbContext.SaveChangesAsync();
            }
        }

        _logger.LogInformation($"\n📈 Summary:");
        _logger.LogInformation($"   Total processed: {processed}");
        _logger.LogInformation($"   Successfully labeled: {successful}");
        _logger.LogInformation($"   Errors: {processed - successful}");
    }

    private async Task<string> GenerateRecipeLabelAsync(Recipe recipe)
    {
        var prompt = RecipeLabelingPromptBuilder.BuildLabelingPrompt(recipe);

        var chatService = _kernel.GetRequiredService<IChatCompletionService>();

        var promptExecutionSettings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
                {
                    { "Temperature", 0.3 },
                    { "MaxTokens", 20 }
                }
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var result = await chatService.GetChatMessageContentAsync(chatHistory, promptExecutionSettings);

        var labeledContent = result.Content?.Trim();

        var validCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Breakfast", "Lunch", "Dinner", "Appetizer", "Dessert", "Snack", "Beverage", "Salad", "Soup", "Main Course", "Side Dish", "Vegetarian", "Vegan", "Healthy", "Comfort Food", "Quick & Easy"
            };

        if (string.IsNullOrEmpty(labeledContent) || !validCategories.Contains(labeledContent))
        {
            _logger.LogWarning($"Invalid or unexpected label '{labeledContent}' received for recipe '{recipe.RecipeName}' (ID: {recipe.RecipeID}). Defaulting to 'Uncategorized'.");
            return "Uncategorized";
        }

        return labeledContent;
    }


}