using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using CraftMarket.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CraftMarket.Controllers
{
    public class MyWorkController : Controller
    {
        /*
         * Code Attribution
         * Dependency injection in ASP.NET Core
         * Rick-Anderson
         * learn.microsoft.com
         * https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-7.0
         */
        private readonly CraftMarketContext _context;
        /*
         * Code Attribution
         * Make HTTP requests using IHttpClientFactory in ASP.NET Core
         * stevejgordon
         * learn.microsoft.com
         * https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-7.0
         */
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _functionAppBaseUrl;
        private readonly UserManager<IdentityUser> _userManager;

        // Constructor for dependency injection
        public MyWorkController(CraftMarketContext context, IHttpClientFactory clientFactory, IConfiguration configuration, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _clientFactory = clientFactory;
            _functionAppBaseUrl = configuration["FunctionAppBaseUrl"] ?? string.Empty;
            _userManager = userManager;
        }

        /*
         * Code Attribution
         * EntityFrameworkQueryableExtensions.Include Method (Microsoft.EntityFrameworkCore)
         * dotnet-bot
         * learn.microsoft.com
         * https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.include?view=efcore-7.0
         */
        // Displays the list of products
        public IActionResult Index()
        {
            var data = _context.Products.Include(p => p.Category).ToList();
            return View(data);
        }

        /*
         * Code Attribution
         * HttpPostAttribute Class (Microsoft.AspNetCore.Mvc)
         * dotnet-bot
         * learn.microsoft.com
         * https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.httppostattribute?view=aspnetcore-7.0
         */
        // Processes product orders
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> OrderProduct(int productId)
        {
            /*
             * Code Attribution
             * UserManager Class (Microsoft.AspNetCore.Identity)
             * dotnet-bot
             * learn.microsoft.com
             * https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1?view=aspnetcore-7.0
             */
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var userEmail = user?.Email;

            if (userId == null || userEmail == null)
            {
                return Challenge();
            }

            var order = new Order
            {
                ProductId = productId,
                UserId = userId,
                OrderStatus = "Pending",
                PaymentStatus = "Pending"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            await SendConfirmationEmail(userEmail, order.OrderId);

            return View("OrderConfirmation");
        }

        // Sends a confirmation email for the order
        public async Task<IActionResult> SendConfirmationEmail(string toEmail, int orderId)
        {
            /*
             * Code Attribution
             * SmtpClient Class (System.Net.Mail)
             * karelz
             * learn.microsoft.com
             * https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-7.0
             */
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("smtp-user@example.com", "CHANGE_ME"),
                    EnableSsl = true,
                };

                /*
                 * Code Attribution
                 * IUrlHelper.Action(UrlActionContext) Method (Microsoft.AspNetCore.Mvc)
                 * dotnet-bot
                 * learn.microsoft.com
                 * https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.iurlhelper.action?view=aspnetcore-7.0
                 */
                var confirmationLink = Url.Action("ConfirmOrderAndPayment", "MyWork", new { orderId }, Request.Scheme);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("noreply@example.com"),
                    Subject = "Confirm your order and payment",
                    Body = $@"
                        <h1>Thank you for your order!</h1>
                        <p>Please confirm your order and payment by clicking the link below:</p>
                        <a href='{confirmationLink}'>Confirm Order and Payment</a>",
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);

                return Ok("Email sent successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error sending email: {ex.Message}");
            }
        }

        // Confirms the order and payment
        public IActionResult ConfirmOrderAndPayment(int orderId)
        {
            var order = GetOrderById(orderId);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // Confirms the order via external API
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(int orderId)
        {
            var client = _clientFactory.CreateClient();
            var response = await client.PostAsync($"{_functionAppBaseUrl}/api/ConfirmOrder?orderId={orderId}", null);

            if (response.IsSuccessStatusCode)
            {
                return Ok("Order confirmed.");
            }
            return BadRequest("Failed to confirm order.");
        }

        // Confirms the payment via external API
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int orderId)
        {
            var client = _clientFactory.CreateClient();
            var response = await client.PostAsync($"{_functionAppBaseUrl}/api/ConfirmPayment?orderId={orderId}", null);

            if (response.IsSuccessStatusCode)
            {
                return Ok("Payment confirmed.");
            }
            return BadRequest("Failed to confirm payment.");
        }

        // Retrieves an order by its ID
        private Order? GetOrderById(int orderId)
        {
            return _context.Orders
                .Include(o => o.Product)
                .FirstOrDefault(o => o.OrderId == orderId);
        }
    }
}
