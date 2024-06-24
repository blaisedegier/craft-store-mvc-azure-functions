using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ServerlessFunc.Models;

namespace OrderProcessing
{
    public static class OrderConfirmationFunction
    {
        /*
         * Code Attribution
         * DbContextOptionsBuilder Class (Microsoft.EntityFrameworkCore)
         * dotnet-bot
         * learn.microsoft.com
         * https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder?view=efcore-7.0
         */
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

                order.OrderStatus = "Confirmed";
                context.Orders.Update(order);
                await context.SaveChangesAsync();
            }

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("Order confirmed.");
            return response;
        }
    }
}
