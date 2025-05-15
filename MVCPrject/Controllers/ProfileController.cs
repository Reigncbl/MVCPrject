using Microsoft.AspNetCore.Mvc;

namespace YourProjectNamespace.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Profile()
        {
            return View();
        }
    }
}
