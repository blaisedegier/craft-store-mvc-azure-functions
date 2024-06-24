using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace OrderProcessing
{
    // Defines the orchestrator function that processes orders.
    public static class OrderProcessingOrchestrator
    {
        // Retrieves the base URL for the function app from environment variables.
        private static readonly string FunctionAppBaseUrl = Environment.GetEnvironmentVariable("FunctionAppBaseUrl") ?? string.Empty;

        /*
         * Code Attribution
         * Microsoft.Azure.Functions.Worker Namespace - Azure for .NET Developers
         * learn.microsoft.com
         * https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.functions.worker?view=azure-dotnet
         */
        [Function(nameof(OrderProcessingOrchestrator))]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            /*
             * Code Attribution
             * Logging in C# - .NET
             * IEvangelist
             * learn.microsoft.com
             * https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line
             */
            // Creates a logger that is safe to use in replay events within durable functions.
            ILogger logger = context.CreateReplaySafeLogger(nameof(OrderProcessingOrchestrator));
            logger.LogInformation("Starting order processing orchestrator.");

            // Retrieves the input for the orchestrator, which is expected to be an order ID.
            var orderId = context.GetInput<int>();

            // Initiates tasks to wait for external events indicating order and payment confirmation.
            var orderConfirmationTask = context.WaitForExternalEvent<bool>("OrderConfirmed");
            var paymentConfirmationTask = context.WaitForExternalEvent<bool>("PaymentConfirmed");

            /*
             * Code Attribution
             * SystemEvents.CreateTimer(Int32) Method (Microsoft.Win32)
             * dotnet-bot
             * learn.microsoft.com
             * https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.systemevents.createtimer?view=net-8.0
             */
            // Creates a timer task that completes after 24 hours, acting as a timeout for the order processing.
            var timeoutTask = context.CreateTimer(context.CurrentUtcDateTime.AddHours(24), CancellationToken.None);

            /*
             * Code Attribution
             * Task.WhenAny Method (System.Threading.Tasks)
             * dotnet-bot
             * learn.microsoft.com
             * https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenany?view=net-7.0
             */
            // Waits for any of the tasks (order confirmation, payment confirmation, or timeout) to complete.
            var completedTask = await Task.WhenAny(orderConfirmationTask, paymentConfirmationTask, timeoutTask);

            // Checks which task completed and handles the outcome accordingly.
            if (completedTask == timeoutTask)
            {
                // If the timeout task completed, logs and handles the timeout scenario.
                logger.LogInformation("Order processing timed out.");
                context.SetCustomStatus("Expired");
                await UpdateOrderStatus(orderId, "Expired");
            }
            else
            {
                // If the order is confirmed, logs and updates the order status.
                if (orderConfirmationTask.IsCompleted)
                {
                    logger.LogInformation("Order confirmed.");
                    context.SetCustomStatus("OrderConfirmed");
                    await UpdateOrderStatus(orderId, "OrderConfirmed");
                }

                // If the payment is confirmed, logs and updates the payment status.
                if (paymentConfirmationTask.IsCompleted)
                {
                    logger.LogInformation("Payment confirmed.");
                    context.SetCustomStatus("PaymentConfirmed");
                    await UpdateOrderStatus(orderId, "PaymentConfirmed");
                }

                // If both order and payment are confirmed, logs and updates the status to completed.
                if (orderConfirmationTask.IsCompleted && paymentConfirmationTask.IsCompleted)
                {
                    logger.LogInformation("Order and payment confirmed.");
                    context.SetCustomStatus("Completed");
                    await UpdateOrderStatus(orderId, "Completed");
                }
            }
        }

        /*
         * Code Attribution
         * HttpClient Class (System.Net.Http)
         * karelz
         * learn.microsoft.com
         * https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=net-7.0
         */
        // Updates the order status by making an HTTP POST request to a specified endpoint.
        private static async Task UpdateOrderStatus(int orderId, string status)
        {
            var client = new HttpClient();
            await client.PostAsJsonAsync<object>($"{FunctionAppBaseUrl}/api/UpdateOrderStatus?orderId={orderId}&status={status}", new { });
        }
    }
}
