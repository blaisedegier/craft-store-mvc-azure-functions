using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using ServerlessFunc.Models;

namespace OrderProcessing
{
    // PaymentProcessingFunction class contains the ConfirmPayment function, which confirms payment for an order.
    public static class PaymentProcessingFunction
    {
        // Creates and configures a new DbContext for database operations, similar to OrderConfirmationFunction.
        private static CraftMarketContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<CraftMarketContext>();
            var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            optionsBuilder.UseSqlServer(connectionString);
            return new CraftMarketContext(optionsBuilder.Options);
        }

        // HTTP-triggered function to confirm payment for an order.
        [Function("ConfirmPayment")]
        public static async Task<HttpResponseData> ConfirmPayment(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            // Initializes logger and parses query string for order ID.
            var logger = executionContext.GetLogger("ConfirmPayment");
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var orderIdString = query["orderId"];

            // Validates the order ID and returns a bad request response if invalid.
            if (string.IsNullOrEmpty(orderIdString) || !int.TryParse(orderIdString, out int orderId))
            {
                var badRequestResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid order ID.");
                return badRequestResponse;
            }

            // Uses DbContext to find and update the payment status in the database.
            using (var context = CreateDbContext())
            {
                var order = context.Orders.FirstOrDefault(o => o.OrderId == orderId);
                if (order == null)
                {
                    var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync("Order not found.");
                    return notFoundResponse;
                }

                order.PaymentStatus = "Confirmed";
                context.Orders.Update(order);
                await context.SaveChangesAsync();
            }

            // Returns an OK response indicating the payment has been confirmed.
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("Payment confirmed.");
            return response;
        }
    }
}
