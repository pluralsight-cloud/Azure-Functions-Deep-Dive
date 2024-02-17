using System;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Pluralsight.AzureFuncs
{
    public record NewOrderMessage(Guid orderId, int productId, int quantity, 
        string customerName, string customerEmail, decimal purchasePrice);

    public class ProcessNewOrder
    {
        private readonly ILogger<ProcessNewOrder> _logger;

        public ProcessNewOrder(ILogger<ProcessNewOrder> logger)
        {
            _logger = logger;
        }

        [Function(nameof(ProcessNewOrder))]
        [BlobOutput("tickets/{orderId}.txt", Connection = "AzureWebJobsStorage")]
        public string Run([QueueTrigger("neworders", Connection = "AzureWebJobsStorage")] 
            NewOrderMessage message)
        {
            var description = $"Order {message.orderId}: " + 
                $"{message.customerName} bought {message.productId}";
            _logger.LogInformation(description);
            return description;
        }
    }
}
