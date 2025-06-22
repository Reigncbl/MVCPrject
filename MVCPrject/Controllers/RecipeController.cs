
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVCPrject.Data;


namespace MVCPrject;

[Route("Recipe")]
public class RecipeController : Controller
{

    private DBContext _context;

    public RecipeController(DBContext context)
    {
        _context = context;
    }


    [Route("All")]
    public async Task<IActionResult> Recipe()
    {
        var recipes = await _context.Recipes.ToListAsync();
        return View(recipes);
    }

    [Route("")]
    public async Task<IActionResult> Recipe()
    {
        var recipes = await _context.Recipes.ToListAsync();
        return View(recipes);
    }



}
