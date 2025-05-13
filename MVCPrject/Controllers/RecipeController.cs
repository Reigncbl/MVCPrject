using Microsoft.AspNetCore.Mvc;
using MVCPrject.Data;
using MVCPrject.Models;

[Route("Recipe")] 
public class RecipeController : Controller
{

    private readonly UrlScraper _scraper;
    private readonly DBContext _context;

    public RecipeController(UrlScraper scraper, DBContext context)
    {
        _scraper = scraper;
        _context = context;
    }

   
    [Route("Recipe")] 
    public async Task<IActionResult> Recipe()
    {
       
        var recipe = await _scraper.ScrapeRecipeDataAsync();

       
        if (recipe == null)
        {
            return View("Error"); 
        }
       
        return View(recipe);
    }
}
