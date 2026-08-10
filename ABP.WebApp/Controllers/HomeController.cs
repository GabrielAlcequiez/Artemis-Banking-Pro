using ABP.WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Error()
        {
            return View(new ErrorViewModel());
        }
    }
}