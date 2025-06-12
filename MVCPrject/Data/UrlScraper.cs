using HtmlAgilityPack;
using MVCPrject.Data;
using MVCPrject.Models;
using Microsoft.EntityFrameworkCore;

public class RecipeScraper
{
    private readonly DBContext _dbContext;
    private readonly HttpClient _httpClient = new();

    public RecipeScraper(DBContext dbContext)
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



   
    public async Task<Recipe?> ScrapeRecipeDataAsync()
    {
        string url = "https://panlasangpinoy.com/filipino-chicken-ala-king/";

        using var http = new HttpClient();

        try
        {
            // Step 1: Scrape the HTML content from the URL
            var html = await http.GetStringAsync(url);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // Step 2: Create and populate the Recipe object
            var recipe = new Recipe();

            // Recipe Name
            recipe.RecipeName = doc.DocumentNode
                .Descendants("h1")
                .FirstOrDefault()?.InnerText?.Trim() ?? "Recipe Name Not Found";

            // Recipe Image
            recipe.RecipeImage = doc.DocumentNode
                .Descendants("img")
                .FirstOrDefault(i => i.GetAttributeValue("src", "").StartsWith("https://panlasangpinoy.com/wp-content/uploads/"))?
                .GetAttributeValue("src","") ?? "/images/default-recipe.jpg";

            // Recipe Ingredients
            var ingredientsList = doc.DocumentNode
                .SelectNodes("//div[contains(@class, 'wprm-recipe-ingredient-group')]")
                .Select(g =>
                    $"{g?.SelectSingleNode("./h4")?.InnerText?.Trim() ?? "General"}:\n" +
                    string.Join("\n", g?.SelectNodes("./ul/li/span[contains(@class, 'wprm-recipe-ingredient-name')]")
                    .Select(i => $" - {i?.InnerText?.Trim()}") ?? new[] { "No ingredients found" })
                ).ToList();
            recipe.RecipeIngredients = string.Join("\n\n", ingredientsList);

            // Recipe Instructions
            var instructions = doc.DocumentNode
                .SelectNodes("//div[contains(@class, 'wprm-recipe-instruction-group')]/ul/li/div[contains(@class, 'wprm-recipe-instruction-text')]")
                .Select((i, index) => $"{index + 1}. {i?.InnerText?.Trim() ?? "No instruction text"}")
                .ToList();
            recipe.RecipeDescription = string.Join("\n", instructions);

            // Step 3: Return the populated Recipe object
            return recipe;
        }
        catch (Exception ex)
        {
            // Handle any errors
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

}


