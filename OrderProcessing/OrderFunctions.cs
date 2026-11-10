using System.Net;
using DurableTask.Core.Exceptions;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Data;

namespace OrderProcessing
{
    // The two HTTP entry points the web app calls. Neither writes to the database itself: ConfirmOrder
    // starts the order's orchestration and ConfirmPayment raises an event on it, and the orchestration
    // makes every status change through its activity.
    public class OrderFunctions
    {
        private readonly OrdersContext _db;

        public OrderFunctions(OrdersContext db)
        {
            _db = db;
        }

        // Adapted from https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview?tabs=in-process%2Cnodejs-v3%2Cv1-model&pivots=csharp
        [Function("ConfirmOrder")]
        public async Task<HttpResponseData> ConfirmOrder(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client)
        {
            if (!TryGetOrderId(req, out var orderId))
            {
                return await TextAsync(req, HttpStatusCode.BadRequest, "Invalid order ID.");
            }

            if (!await _db.Orders.AnyAsync(o => o.OrderId == orderId))
            {
                return await TextAsync(req, HttpStatusCode.NotFound, "Order not found.");
            }

            var instanceId = OrderProcessingOrchestrator.InstanceIdFor(orderId);
            if (await client.GetInstanceAsync(instanceId) != null)
            {
                return await TextAsync(req, HttpStatusCode.Conflict, "This order has already been confirmed.");
            }

            try
            {
                await client.ScheduleNewOrchestrationInstanceAsync(
                    nameof(OrderProcessingOrchestrator), orderId, new StartOrchestrationOptions(instanceId));
            }
            catch (Exception ex) when (IsAlreadyExists(ex))
            {
                // Two requests for the same order can both pass the check above; the host starts only one.
                return await TextAsync(req, HttpStatusCode.Conflict, "This order has already been confirmed.");
            }

            return await TextAsync(req, HttpStatusCode.Accepted, "Order confirmed. Please confirm payment within 24 hours.");
        }

        [Function("ConfirmPayment")]
        public async Task<HttpResponseData> ConfirmPayment(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client)
        {
            if (!TryGetOrderId(req, out var orderId))
            {
                return await TextAsync(req, HttpStatusCode.BadRequest, "Invalid order ID.");
            }

            var instanceId = OrderProcessingOrchestrator.InstanceIdFor(orderId);
            var instance = await client.GetInstanceAsync(instanceId);
            if (instance == null)
            {
                return await TextAsync(req, HttpStatusCode.Conflict, "Confirm the order before confirming payment.");
            }

            if (instance.RuntimeStatus is not (OrchestrationRuntimeStatus.Pending or OrchestrationRuntimeStatus.Running))
            {
                return await TextAsync(req, HttpStatusCode.Conflict, "This order is no longer awaiting payment.");
            }

            await client.RaiseEventAsync(instanceId, OrderProcessingOrchestrator.PaymentConfirmedEvent, true);
            return await TextAsync(req, HttpStatusCode.Accepted, "Payment confirmed.");
        }

        // The host refuses to start an instance id that is already running (host.json sets
        // overridableExistingInstanceStates to NonRunningStates). The gRPC client normally reports that as
        // OrchestrationAlreadyExistsException; the RpcException check covers the status arriving unwrapped.
        private static bool IsAlreadyExists(Exception ex) =>
            ex is OrchestrationAlreadyExistsException
            || ex is RpcException { StatusCode: StatusCode.AlreadyExists };

        // Adapted from https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.functions.worker.http.httprequestdata?view=azure-dotnet
        private static bool TryGetOrderId(HttpRequestData req, out int orderId)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            return int.TryParse(query["orderId"], out orderId);
        }

        // Adapted from https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.functions.worker.http.httpresponsedata?view=azure-dotnet
        private static async Task<HttpResponseData> TextAsync(HttpRequestData req, HttpStatusCode status, string message)
        {
            var response = req.CreateResponse(status);
            await response.WriteStringAsync(message);
            return response;
        }
    }
}
