using System.Net.Http;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
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

                var links = doc.DocumentNode.SelectNodes("//h2[@class='entry-title']/a")
                    ?.Select(n => n.GetAttributeValue("href", "").Trim())
                    .Where(link => !string.IsNullOrEmpty(link) && link.StartsWith("https://"))
                    .Distinct()
                    .ToList();

                if (links == null || !links.Any()) break;

                foreach (var link in links)
                {
                    if (!await _dbContext.Recipes.AnyAsync(r => r.RecipeURL == link))
                    {
                        _dbContext.Recipes.Add(new Recipe
                        {
                            RecipeURL = link
                        });
                    }
                }

                await _dbContext.SaveChangesAsync();
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error on page {page}: {ex.Message}");
                break;
            }
        }

        Console.WriteLine($"Done scraping category: {category}");
    }
    public async Task LoopUrlAsync()
    {
        var recipes = await _dbContext.Recipes.ToListAsync();

        Console.WriteLine($"Found {recipes.Count} recipes to scrape.");

        foreach (var recipe in recipes)
        {
            try
            {
                Console.WriteLine($"Scraping: {recipe.RecipeURL}");
                await RecipeInfoRetrieverAsync(recipe.RecipeURL);
                Console.WriteLine($"✓ {recipe.RecipeName}");
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed: {ex.Message}");
            }
        }

        await _dbContext.SaveChangesAsync();
        Console.WriteLine("Done!");
    }


    public async Task RecipeInfoRetrieverAsync(string url)
    {
        using var http = new HttpClient();
        try
        {
            var recipe = await _dbContext.Recipes.FirstOrDefaultAsync(r => r.RecipeURL == url);
            if (recipe == null) return;

            var html = await http.GetStringAsync(url);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // Scrape the data
            string name = doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'entry-title')]")?.InnerText?.Trim();
            string description = doc.DocumentNode.SelectSingleNode("(//p)[5]")?.InnerText?.Trim();
            string img = doc.DocumentNode.Descendants("img").FirstOrDefault(i => i.GetAttributeValue("src", "").StartsWith("https://panlasangpinoy.com/wp-content/uploads/"))?.GetAttributeValue("src", "");

            // Build ingredients string
            var ingredients = new List<string>();
            foreach (var g in doc.DocumentNode.SelectNodes("//div[contains(@class, 'wprm-recipe-ingredient-group')]") ?? new HtmlNodeCollection(null))
            {
                string groupName = g?.SelectSingleNode("./h4")?.InnerText?.Trim() ?? "General";
                ingredients.Add($"{groupName}:");
                foreach (var i in g?.SelectNodes("./ul/li/span[contains(@class, 'wprm-recipe-ingredient-name')]") ?? new HtmlNodeCollection(null))
                    ingredients.Add($"- {i?.InnerText?.Trim()}");
            }

            // Build instructions string
            var instructions = new List<string>();
            int s = 1;
            foreach (var i in doc.DocumentNode.SelectNodes("//div[contains(@class, 'wprm-recipe-instruction-group')]/ul/li/div[contains(@class, 'wprm-recipe-instruction-text')]") ?? new HtmlNodeCollection(null))
                instructions.Add($"{s++}. {i?.InnerText?.Trim()}");

            // Update the recipe in database
            recipe.RecipeName = name;
            recipe.RecipeDescription = description;
            recipe.RecipeImage = img;
            recipe.RecipeIngredients = string.Join("\n", ingredients);
            recipe.RecipeInstructions = string.Join("\n", instructions);

            // Note: Don't call SaveChanges here - let the loop handle it
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        // Remove Console.ReadKey() since this is called in a loop
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





}

