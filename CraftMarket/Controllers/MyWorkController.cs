using CraftMarket.Data;
using CraftMarket.Models;
using CraftMarket.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CraftMarket.Controllers
{
    // The product catalogue, ordering, and the confirm-order and confirm-payment steps that hand off
    // to the OrderProcessing Functions app.
    public class MyWorkController : Controller
    {
        // Adapted from https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-7.0
        private readonly CraftMarketContext _context;
        // Adapted from https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-7.0
        private readonly IHttpClientFactory _clientFactory;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly OrderEmailSender _emailSender;
        private readonly ILogger<MyWorkController> _logger;
        private readonly string _functionAppBaseUrl;
        private readonly string? _functionAppKey;

        public MyWorkController(
            CraftMarketContext context,
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            UserManager<IdentityUser> userManager,
            OrderEmailSender emailSender,
            ILogger<MyWorkController> logger)
        {
            _context = context;
            _clientFactory = clientFactory;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            _functionAppBaseUrl = (configuration["FunctionAppBaseUrl"] ?? string.Empty).TrimEnd('/');
            _functionAppKey = configuration["FunctionAppKey"];
        }

        // Adapted from https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.include?view=efcore-7.0
        public IActionResult Index()
        {
            var data = _context.Products.Include(p => p.Category).ToList();
            return View(data);
        }

        // Creates a pending order for the signed-in user and emails them the confirmation link.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderProduct(int productId)
        {
            // Adapted from https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1?view=aspnetcore-7.0
            var user = await _userManager.GetUserAsync(User);
            if (user?.Email == null)
            {
                return Challenge();
            }

            if (!await _context.Products.AnyAsync(p => p.ProductId == productId))
            {
                return NotFound();
            }

            var order = new Order
            {
                ProductId = productId,
                UserId = user.Id,
                OrderStatus = OrderStatuses.Pending,
                PaymentStatus = OrderStatuses.Pending
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Adapted from https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.iurlhelper.action?view=aspnetcore-7.0
            var confirmationLink = Url.Action(nameof(ConfirmOrderAndPayment), "MyWork", new { orderId = order.OrderId }, Request.Scheme)!;
            var emailSent = await _emailSender.SendOrderConfirmationAsync(user.Email, confirmationLink);

            // Without SMTP configured the link is shown on the page instead, so the flow can be run locally.
            ViewBag.ConfirmationLink = emailSent ? null : confirmationLink;
            return View("OrderConfirmation");
        }

        // The page the emailed link opens. Only the customer who placed the order can see it.
        [Authorize]
        public async Task<IActionResult> ConfirmOrderAndPayment(int orderId)
        {
            var order = await FindOwnOrderAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // Starts the order-processing orchestration in the Functions app.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ConfirmOrder(int orderId) =>
            ForwardToFunctionAsync(orderId, "ConfirmOrder");

        // Tells the running orchestration that payment has been made.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ConfirmPayment(int orderId) =>
            ForwardToFunctionAsync(orderId, "ConfirmPayment");

        private async Task<IActionResult> ForwardToFunctionAsync(int orderId, string functionName)
        {
            if (await FindOwnOrderAsync(orderId) == null)
            {
                return NotFound();
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_functionAppBaseUrl}/api/{functionName}?orderId={orderId}");
            if (!string.IsNullOrEmpty(_functionAppKey))
            {
                request.Headers.Add("x-functions-key", _functionAppKey);
            }

            try
            {
                var response = await _clientFactory.CreateClient().SendAsync(request);
                TempData["Message"] = await response.Content.ReadAsStringAsync();
                TempData["MessageIsError"] = !response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Calling {Function} failed.", functionName);
                TempData["Message"] = "The order processing service is not reachable. Please try again later.";
                TempData["MessageIsError"] = true;
            }

            return RedirectToAction(nameof(ConfirmOrderAndPayment), new { orderId });
        }

        private async Task<Order?> FindOwnOrderAsync(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            return await _context.Orders
                .Include(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
        }
    }
}
