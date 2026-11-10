using CraftMarket.Models;
using Microsoft.EntityFrameworkCore;

namespace CraftMarket.Data
{
    // Adapted from https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/working-with-sql?view=aspnetcore-8.0&tabs=visual-studio
    public static class SeedData
    {
        // Adds the starter categories and products to an empty catalogue. The schema must already exist.
        public static void Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<CraftMarketContext>();

            if (context.Products.Any() || context.Categories.Any())
            {
                return;
            }

            context.Categories.AddRange(
                new Category
                {
                    Name = "Wooden",
                    Description = "The category wooden encompasses items and structures made primarily from wood, highlighting the natural beauty and durability of this versatile material."
                },
                new Category
                {
                    Name = "Ornament",
                    Description = "The ornament category features decorative items designed to enhance the aesthetic appeal of spaces, often used during festive seasons or as permanent embellishments."
                },
                new Category
                {
                    Name = "Jewelry",
                    Description = "The jewelry category includes a wide range of personal adornments, such as necklaces, bracelets, rings, and earrings, crafted from precious metals and gemstones."
                }
            );
            context.SaveChanges();

            var woodenCategory = context.Categories.FirstOrDefault(c => c.Name == "Wooden");
            var ornamentCategory = context.Categories.FirstOrDefault(c => c.Name == "Ornament");
            var jewelryCategory = context.Categories.FirstOrDefault(c => c.Name == "Jewelry");
            if (woodenCategory != null && ornamentCategory != null && jewelryCategory != null)
            {
                context.Products.AddRange(
                    new Product
                    {
                        Name = "Beadwork",
                        Description = "African handcrafted beadwork items are intricate, vibrant pieces of art created using traditional techniques that reflect the rich cultural heritage and craftsmanship of various African communities.",
                        Price = 49,
                        Availability = "In Stock",
                        Image = "/images/beadwork.jpg",
                        CategoryId = jewelryCategory.CategoryId
                    },
                    new Product
                    {
                        Name = "Wood Carvings",
                        Description = "African handcrafted wood carvings are detailed, culturally significant artworks created using traditional techniques that showcase the rich heritage and skilled craftsmanship of various African communities.",
                        Price = 29,
                        Availability = "In Stock",
                        Image = "/images/woodCarving.jpg",
                        CategoryId = woodenCategory.CategoryId
                    },
                    new Product
                    {
                        Name = "Textiles",
                        Description = "African handcrafted textiles are vibrant, culturally rich fabrics made using traditional techniques, reflecting the unique heritage and artistry of various African communities.",
                        Price = 99,
                        Availability = "In Stock",
                        Image = "/images/textiles.jpg",
                        CategoryId = woodenCategory.CategoryId
                    },
                    new Product
                    {
                        Name = "Pottery",
                        Description = "African handcrafted pottery consists of intricately designed and culturally significant ceramics, created using traditional techniques that highlight the artistic heritage of various African communities.",
                        Price = 79,
                        Availability = "In Stock",
                        Image = "/images/pottery.jpg",
                        CategoryId = ornamentCategory.CategoryId
                    },
                    new Product
                    {
                        Name = "Jewelry",
                        Description = "African handcrafted jewelry features intricately designed pieces made from diverse materials, showcasing the rich cultural heritage and skilled artistry of various African communities.",
                        Price = 199,
                        Availability = "In Stock",
                        Image = "/images/jewelry.jpg",
                        CategoryId = jewelryCategory.CategoryId
                    },
                    new Product
                    {
                        Name = "Sculptures",
                        Description = "African handcrafted sculptures are detailed, culturally significant artworks carved from various materials, reflecting the rich heritage and skilled craftsmanship of African communities.",
                        Price = 99,
                        Availability = "In Stock",
                        Image = "/images/sculptures.jpg",
                        CategoryId = ornamentCategory.CategoryId
                    }
                );
                context.SaveChanges();
            }
        }
    }
}
