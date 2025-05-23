using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MVCPrject.Data;
using MVCPrject.Models;

namespace MVCPrject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DBContext _context;


        public HomeController(ILogger<HomeController> logger, DBContext context)
        {
            _logger = logger;
            _context = context;
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}