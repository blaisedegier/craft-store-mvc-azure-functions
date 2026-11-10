using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderProcessing.Data;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // SqlConnectionString comes from local.settings.json locally and from app settings in Azure.
        services.AddDbContext<OrdersContext>(options =>
            options.UseSqlServer(context.Configuration["SqlConnectionString"]));
    })
    .Build();

host.Run();
