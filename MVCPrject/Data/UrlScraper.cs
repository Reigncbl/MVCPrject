using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Linq;
using System.Collections.Generic;
using MVCPrject.Models;
using System.Text;

public class UrlScraper
{
    //chicken-recipes
    //pork-recipes
    //dessert-and-pastry-recipes
    //beef-recipes
    //vegetable-recipes
    //fish-recipes-recipes
    //fish-recipes-recipes
    //pasta-recipes/
    //rice-recipes
    //eggs
    //tofu-recipes-recipes/
    //noodle-recipes/

    public static async Task<String> URLScraper(string catUrl)
    {
        string baseUrl = "https://panlasangpinoy.com/categories/recipes/"+catUrl;
        var allUrls = new List<string>();
        for (int page = 1; page <= 5; page++) 
        {
            string url = $"{baseUrl}page/{page}/";
            try
            {
                var html = await new HttpClient().GetStringAsync(url);
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                doc.DocumentNode.SelectNodes("//h2[@class='entry-title']/a")?
                   .Select(n => n?.GetAttributeValue("href", "").Trim())
                   .Where(s => !string.IsNullOrWhiteSpace(s) && s.StartsWith("https://panlasangpinoy.com/"))
                   .ToList()?.ForEach(u => allUrls.Add(u));
                await Task.Delay(500);
                if (!allUrls.Any() && page > 1 ) break;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error scraping page {page}: {exception.Message}");
                break;
            }

        }
        return allUrls.ToString();
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