using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}