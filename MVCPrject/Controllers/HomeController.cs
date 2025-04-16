using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MVCPrject.Models;

namespace MVCPrject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var item = "hello world";

            return View(item);
        }

 
    }
}
