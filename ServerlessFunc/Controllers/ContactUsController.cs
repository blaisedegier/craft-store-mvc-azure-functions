using Microsoft.AspNetCore.Mvc;
using ServerlessFunc.Models;
using System.Diagnostics;

namespace ServerlessFunc.Controllers
{
    public class ContactUsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        /*
         * Code Attribution
         * Bipin Paul
         * CodeProject
         * How to Implement Contact Us Page in ASP.NET MVC (ASP.NET 5 )
         * 28 February 2016
         * https://www.codeproject.com/tips/1081578/how-to-implement-contact-us-page-in-asp-net-mvc-as
         */
        [HttpPost]
        public IActionResult Index(ContactUs contactUs)
        {
            if (ModelState.IsValid)
            {
                // Save the contactUs object to the database
                ViewBag.Message = "Message successfully sent.";
            }
            return View(contactUs);
        }
    }
}
