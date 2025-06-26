using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using MVCPrject.Data;
using MVCPrject.Models;
using System;
using System.Threading.Tasks;

namespace MVCPrject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DBContext _context;
        private readonly IDistributedCache _cache;

        public HomeController(ILogger<HomeController> logger, DBContext context, IDistributedCache cache)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        public IActionResult Home()
        {
            return View();
        }

        public IActionResult Newview()
        {
            return View();
        }

        public IActionResult Recipe()
        {
            return View();
        }

        public IActionResult CreateEditRecipe()
        {
            return View();
        }

        public IActionResult CreateEditRecipeForm(Recipe model)
        {
            _context.Recipes.Add(model);
            _context.SaveChanges();
            return RedirectToAction("Recipe");
        }


        [HttpGet("/inspect-cache")]
        public async Task<IActionResult> InspectCache([FromQuery] string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return BadRequest("Please provide a cache key as ?key=...");

            var cachedData = await _cache.GetStringAsync(key);
            if (cachedData == null)
                return Ok($"Cache key: {key}\nValue: (not found)");

            return Content($"Cache key: {key}\nValue: {cachedData}", "text/plain");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }




}
