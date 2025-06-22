
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVCPrject.Data;


namespace MVCPrject;

[Route("Recipe")]
public class RecipeController : Controller
{
    private readonly RecipeManipulationService _repository;
    public RecipeController(RecipeManipulationService repository)
    {
        _repository = repository;
    }


    [Route("All")]
    public async Task<IActionResult> Recipe()
    {
        var recipes =  await _repository.ReadAllRecipes();
        return View(recipes);
    }


    [Route("Details/{id:int}")]
    public async Task<IActionResult> RecipeSingle(int id)
    {
        var recipe = await _repository.ReadRecipe(id);
        if (recipe == null)
        {
            return NotFound();
        }
        return View(recipe);
    }




}
