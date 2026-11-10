using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CraftMarket.Data;
using CraftMarket.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CraftMarket.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly CraftMarketContext _context;

        public AdminController(CraftMarketContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Product)
                .Include(o => o.User)
                .ToListAsync();

            return View(orders);
        }
    }
}
