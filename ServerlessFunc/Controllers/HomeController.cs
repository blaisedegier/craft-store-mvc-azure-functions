using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerlessFunc.Models;
using System.Diagnostics;

namespace ServerlessFunc.Controllers
{
    public class HomeController : Controller
    {
        private readonly CraftMarketContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, CraftMarketContext context)
        {
            _logger = logger;
            _context = context;
        }

        /*
         * Code Attribution
         * CascadingMvcDemo
         * bhrugen
         * 1 April 2023
         * GitHub
         * https://github.com/bhrugen/CascadingMvcDemo
         */
        public IActionResult Index()
        {
            var products = _context.Products.Include(p => p.Category).ToList();
            return View(products);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
