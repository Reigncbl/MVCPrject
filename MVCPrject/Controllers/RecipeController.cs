using Microsoft.AspNetCore.Mvc;
using MVCPrject.Data;


public class RecipesController : Controller
{
    public readonly RecipeContext _context;

    public RecipesController(RecipeContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var recipes = _context.Recipes.ToList();
        return View(recipes);
    }
}
