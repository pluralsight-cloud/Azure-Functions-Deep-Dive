using System;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Pluralsight.AzureFuncs
{
    public record NewOrderMessage(int productId, int quantity, string customerName, 
        string customerEmail, decimal purchasePrice);

    public class ProcessNewOrder
    {
        private readonly ILogger<ProcessNewOrder> _logger;

        public ProcessNewOrder(ILogger<ProcessNewOrder> logger)
        {
            _logger = logger;
        }

        [Function(nameof(ProcessNewOrder))]
        public void Run([QueueTrigger("neworders", Connection = "AzureWebJobsStorage")] 
            NewOrderMessage message)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {message.customerName} bought {message.productId}");
        }
    }
}
