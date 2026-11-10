using System.ComponentModel.DataAnnotations;

namespace CraftMarket.Models
{
    public class ContactUs
    {
        // Adapted from https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/validation?view=aspnetcore-8.0
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
