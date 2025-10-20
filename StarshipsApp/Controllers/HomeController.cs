using Microsoft.AspNetCore.Mvc;

namespace StarshipsApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}