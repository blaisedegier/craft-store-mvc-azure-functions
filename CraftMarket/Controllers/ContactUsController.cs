using Microsoft.AspNetCore.Mvc;
using CraftMarket.Models;
using System.Diagnostics;

namespace CraftMarket.Controllers
{
    public class ContactUsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // Adapted from https://www.codeproject.com/tips/1081578/how-to-implement-contact-us-page-in-asp-net-mvc-as
        // The form is validated but the message is not stored or forwarded anywhere yet.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(ContactUs contactUs)
        {
            if (ModelState.IsValid)
            {
                ViewBag.Message = "Thank you, your message has been received.";
            }
            return View(contactUs);
        }
    }
}
