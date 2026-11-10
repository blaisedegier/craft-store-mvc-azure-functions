using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using CraftMarket.Models;

namespace OrderProcessing
{
    // Function to confirm an order.
    public static class OrderConfirmationFunction
    {
        /*
         * Code Attribution
         * DbContextOptionsBuilder Class (Microsoft.EntityFrameworkCore)
         * dotnet-bot
         * learn.microsoft.com
         * https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder?view=efcore-7.0
         */
        // Creates and configures a new DbContext for database operations.
        private static CraftMarketContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<CraftMarketContext>();
            var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            optionsBuilder.UseSqlServer(connectionString);
            return new CraftMarketContext(optionsBuilder.Options);
        }

        /*
         * Code Attribution
         * Durable Functions Overview - Azure
         * cgillum
         * learn.microsoft.com
         * https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview?tabs=in-process%2Cnodejs-v3%2Cv1-model&pivots=csharp
         */
        // HTTP-triggered function to confirm an order.
        [Function("ConfirmOrder")]
        public static async Task<HttpResponseData> ConfirmOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            /*
             * Code Attribution
             * HttpRequestData Class (Microsoft.Azure.Functions.Worker.Http) - Azure for .NET Developers
             * azure-sdk
             * https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.functions.worker.http.httprequestdata?view=azure-dotnet
             */
            // Initializes logger and parses query string for order ID.
            var logger = executionContext.GetLogger("ConfirmOrder");
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var orderIdString = query["orderId"];

            /*
             * Code Attribution
             * HttpResponseData Class (Microsoft.Azure.Functions.Worker.Http) - Azure for .NET Developers
             * azure-sdk
             * learn.microsoft.com
             * https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.functions.worker.http.httpresponsedata?view=azure-dotnet
             */
            // Validates the order ID and returns a bad request response if invalid.
            if (string.IsNullOrEmpty(orderIdString) || !int.TryParse(orderIdString, out int orderId))
            {
                var badRequestResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid order ID.");
                return badRequestResponse;
            }

            // Uses DbContext to find and update the order status in the database.
            using (var context = CreateDbContext())
            {
                var order = context.Orders.FirstOrDefault(o => o.OrderId == orderId);
                if (order == null)
                {
                    var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync("Order not found.");
                    return notFoundResponse;
                }

                order.OrderStatus = "Confirmed";
                context.Orders.Update(order);
                await context.SaveChangesAsync();
            }

            // Returns an OK response indicating the order has been confirmed.
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("Order confirmed.");
            return response;
        }
    }
}
