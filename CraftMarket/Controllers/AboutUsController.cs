using Microsoft.AspNetCore.Mvc;

namespace CraftMarket.Controllers
{
    public class AboutUsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
