using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ServerlessFunc.Models;

namespace OrderProcessing
{
    public static class PaymentProcessingFunction
    {
        private static CraftMarketContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<CraftMarketContext>();
            var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            optionsBuilder.UseSqlServer(connectionString);
            return new CraftMarketContext(optionsBuilder.Options);
        }

        [Function("ConfirmPayment")]
        public static async Task<HttpResponseData> ConfirmPayment(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("ConfirmPayment");
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var orderIdString = query["orderId"];

            if (string.IsNullOrEmpty(orderIdString) || !int.TryParse(orderIdString, out int orderId))
            {
                var badRequestResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid order ID.");
                return badRequestResponse;
            }

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

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("Payment confirmed.");
            return response;
        }
    }
}
