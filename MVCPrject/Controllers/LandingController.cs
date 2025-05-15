using Microsoft.AspNetCore.Mvc;

namespace YourProjectNamespace.Controllers
{
    public class LandingController : Controller
    {
        // GET: /Landing/
        public IActionResult Index()
        {
            return View();
        }
    }
}
