using Microsoft.AspNetCore.Mvc;

namespace YourProjectNamespace.Controllers
{
    public class MealPlannerController : Controller
    {
        public IActionResult MealPlanner()
        {
            return View();
        }
    }
}
