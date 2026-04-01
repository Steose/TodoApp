using System.Diagnostics; // Activity
using Microsoft.AspNetCore.Mvc; // MVC
using TodoApp.Models; // ErrorViewModel

namespace TodoApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Redirect home page to Todo list
            return RedirectToAction("Index", "Todo");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}