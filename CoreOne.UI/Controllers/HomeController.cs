using CoreOne.UI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CoreOne.UI.Controllers
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
            return View();
        }
        public IActionResult AdminDashboard()
        {
            return View();
        }
        public IActionResult UserDashboard()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode, string errorMessage)
        {
            int finalStatusCode = statusCode ?? 500;
            string finalErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Internal server error" : errorMessage;

            ViewBag.StatusCode = finalStatusCode;
            ViewBag.ErrorMessage = finalErrorMessage;

            return View();
        }
    }
}
