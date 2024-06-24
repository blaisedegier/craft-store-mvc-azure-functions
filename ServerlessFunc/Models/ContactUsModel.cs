using System.ComponentModel.DataAnnotations;

namespace ServerlessFunc.Models
{
    public class ContactUs
    {
        /* 
         * Code Attribution
         * wadepickett
         * learn.microsoft.com
         * Part 9, add validation to an ASP.NET Core MVC app
         * 14 November 2023
         * https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/validation?view=aspnetcore-8.0
         */
        public int Id { get; set; }
        [Required]
        [StringLength(20, MinimumLength = 2)]
        public string? FirstName { get; set; }
        [Required]
        [StringLength(20, MinimumLength = 2)]
        public string? LastName { get; set; }
        [Required]
        public string? Email { get; set; }
        [StringLength(10)]
        public string? CellNumber { get; set; }
    }
}
