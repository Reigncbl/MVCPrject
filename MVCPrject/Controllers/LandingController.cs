using Microsoft.AspNetCore.Mvc;

namespace YourProjectNamespace.Controllers
{
    public class LandingController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        public IActionResult Signin()
        {
            return View();
        }
    }
}
