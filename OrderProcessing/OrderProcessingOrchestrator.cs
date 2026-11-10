using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderProcessing.Data;

namespace OrderProcessing
{
    // One orchestration per order, started by ConfirmOrder. It marks the order confirmed, then waits up to
    // 24 hours for the PaymentConfirmed event raised by ConfirmPayment. Payment in time completes the
    // order and confirms the payment; otherwise both the order and its payment are marked Expired. Every database write happens in the UpdateOrderStatus activity,
    // because orchestrator code is replayed and must not perform I/O itself.
    // Adapted from https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.functions.worker?view=azure-dotnet
    public static class OrderProcessingOrchestrator
    {
        public const string PaymentConfirmedEvent = "PaymentConfirmed";

        private static readonly TimeSpan PaymentWindow = TimeSpan.FromHours(24);

        // A predictable instance id lets ConfirmPayment find the orchestration from the order id alone.
        public static string InstanceIdFor(int orderId) => $"order-{orderId}";

        [Function(nameof(OrderProcessingOrchestrator))]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            // Adapted from https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line
            ILogger logger = context.CreateReplaySafeLogger(nameof(OrderProcessingOrchestrator));
            var orderId = context.GetInput<int>();

            await context.CallActivityAsync(nameof(OrderStatusActivity.UpdateOrderStatus),
                new OrderStatusUpdate(orderId, OrderStatuses.Confirmed, null));
            context.SetCustomStatus("AwaitingPayment");
            logger.LogInformation("Order {OrderId} confirmed; waiting for payment.", orderId);

            // Adapted from https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenany?view=net-7.0
            using var timeoutCts = new CancellationTokenSource();
            var paymentTask = context.WaitForExternalEvent<bool>(PaymentConfirmedEvent);
            var timeoutTask = context.CreateTimer(context.CurrentUtcDateTime.Add(PaymentWindow), timeoutCts.Token);

            if (await Task.WhenAny(paymentTask, timeoutTask) == paymentTask)
            {
                // Cancel the timer so the orchestration can finish instead of waiting for it to fire.
                timeoutCts.Cancel();
                await context.CallActivityAsync(nameof(OrderStatusActivity.UpdateOrderStatus),
                    new OrderStatusUpdate(orderId, OrderStatuses.Completed, OrderStatuses.Confirmed));
                context.SetCustomStatus(OrderStatuses.Completed);
                logger.LogInformation("Order {OrderId} paid and completed.", orderId);
                return OrderStatuses.Completed;
            }

            await context.CallActivityAsync(nameof(OrderStatusActivity.UpdateOrderStatus),
                new OrderStatusUpdate(orderId, OrderStatuses.Expired, OrderStatuses.Expired));
            context.SetCustomStatus(OrderStatuses.Expired);
            logger.LogInformation("Order {OrderId} expired without payment.", orderId);
            return OrderStatuses.Expired;
        }
    }

    // A status change for one order. A null field is left as it is.
    public record OrderStatusUpdate(int OrderId, string? OrderStatus, string? PaymentStatus);

    public class OrderStatusActivity
    {
        private readonly OrdersContext _db;
        private readonly ILogger<OrderStatusActivity> _logger;

        public OrderStatusActivity(OrdersContext db, ILogger<OrderStatusActivity> logger)
        {
            _db = db;
            _logger = logger;
        }

        [Function(nameof(UpdateOrderStatus))]
        public async Task UpdateOrderStatus([ActivityTrigger] OrderStatusUpdate update)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == update.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} no longer exists; status not updated.", update.OrderId);
                return;
            }

            if (update.OrderStatus != null)
            {
                order.OrderStatus = update.OrderStatus;
            }
            if (update.PaymentStatus != null)
            {
                order.PaymentStatus = update.PaymentStatus;
            }
            await _db.SaveChangesAsync();
        }
    }
}
