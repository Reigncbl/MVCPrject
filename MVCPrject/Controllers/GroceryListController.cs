
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVCPrject.Data;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace MVCPrject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroceryListController : ControllerBase
    {
        private readonly RecipeManipulationService _recipeService;
        private readonly ILogger<GroceryListController> _logger;

        public GroceryListController(RecipeManipulationService recipeService, ILogger<GroceryListController> logger)
        {
            _recipeService = recipeService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetGroceryList(DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("API: GetGroceryList endpoint hit with startDate: {startDate} and endDate: {endDate}", startDate, endDate);
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("API: User is not authenticated. Returning Unauthorized.");
                    return Unauthorized();
                }

                _logger.LogInformation("API: User authenticated with ID: {userId}", userId);

                var ingredients = await _recipeService.GetIngredientsFromMealLogAsync(userId, startDate, endDate);
                _logger.LogInformation("API: Found {count} ingredients for user {userId}", ingredients.Count, userId);

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                _logger.LogInformation("API: Sending ingredient data to client.");
                return new JsonResult(ingredients, jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: An error occurred while getting the grocery list.");
                return BadRequest(ex.Message);
            }
        }
    }
}
