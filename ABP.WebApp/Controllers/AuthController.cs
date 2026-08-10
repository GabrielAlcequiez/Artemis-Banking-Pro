using ABP.Application.Common.Validation.Users;
using ABP.WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace ABP.WebApp.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

    }
}