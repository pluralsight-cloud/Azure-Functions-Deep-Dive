using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Pluralsight.AzureFuncs
{
    public class OrderOrchestrationInput
    {
        public Guid OrderId { get; set; }
        public string[] SeatNumbers { get; set; } = Array.Empty<string>();
    }

    public static class NewOrderOrchestration
    {
        [Function(nameof(NewOrderOrchestration))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(NewOrderOrchestration));
            logger.LogInformation("Creating tickets for new order.");

            var orderInput = context.GetInput<OrderOrchestrationInput>()!;

            var tasks = new List<Task<string>>();
            foreach(var seat in orderInput.SeatNumbers)
            {
                tasks.Add(context.CallActivityAsync<string>(nameof(CreateTicket), seat));
            }
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        [Function(nameof(CreateTicket))]
        public static async Task<string> CreateTicket([ActivityTrigger] string seatNumber, 
            [BlobInput("tickets", Connection ="AzureWebJobsStorage")] BlobContainerClient ticketsClient,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("CreateTicket");
            logger.LogInformation("Creating ticket for {seat}.", seatNumber);
            // simulate taking time to create a ticket
            await Task.Delay(5000);
            return $"Ticket for seat {seatNumber}!";
        }

        [Function("NewOrderOrchestration_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("NewOrderOrchestration_HttpStart");
            var inputData = new OrderOrchestrationInput
            {
                OrderId = Guid.NewGuid(),
                SeatNumbers = ["A1", "A2", "A3"]
            };

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(NewOrderOrchestration), inputData);

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
