using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using ServerlessFunc.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ServerlessFunc.Controllers
{
    public class MyWorkController : Controller
    {
        private readonly CraftMarketContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _functionAppBaseUrl;
        private readonly UserManager<IdentityUser> _userManager;

        public MyWorkController(CraftMarketContext context, IHttpClientFactory clientFactory, IConfiguration configuration, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _clientFactory = clientFactory;
            _functionAppBaseUrl = configuration["FunctionAppBaseUrl"] ?? string.Empty;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var data = _context.Products.Include(p => p.Category).ToList();
            return View(data);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> OrderProduct(int productId)
        {
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

        public async Task<IActionResult> SendConfirmationEmail(string toEmail, int orderId)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("smtp-user@example.com", "CHANGE_ME"),
                    EnableSsl = true,
                };

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

        public IActionResult ConfirmOrderAndPayment(int orderId)
        {
            var order = GetOrderById(orderId);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

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

        private Order? GetOrderById(int orderId)
        {
            return _context.Orders
                .Include(o => o.Product)
                .FirstOrDefault(o => o.OrderId == orderId);
        }
    }
}
