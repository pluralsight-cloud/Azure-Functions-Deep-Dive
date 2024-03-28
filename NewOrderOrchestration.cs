using System.Net;
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
        public decimal TotalPrice { get; set; }
    }

    public class ApprovalResult
    {
        public bool IsApproved { get; set; }
        public string ApprovedBy { get;set; } = String.Empty;
    }

    public static class NewOrderOrchestration
    {
        public const string OrderApprovalEventName = "OrderApproval";
    
        [Function(nameof(NewOrderOrchestration))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(NewOrderOrchestration));
            logger.LogInformation("Creating tickets for new order.");

            var orderInput = context.GetInput<OrderOrchestrationInput>()!;

            if (orderInput.TotalPrice >= 500)
            {
                logger.LogInformation("Order is expensive, get manual approval");
                await context.CallActivityAsync(nameof(RequestApproval), orderInput.OrderId);
                try
                {
                    var result = await context.WaitForExternalEvent<ApprovalResult>(
                        OrderApprovalEventName,
                        TimeSpan.FromMinutes(1)
                    );
                    if (!result.IsApproved)
                    {
                        logger.LogWarning("Order was not approved");
                        return [$"Order was not approved (approver: {result.ApprovedBy})"];
                    }
                }
                catch (TaskCanceledException) 
                {
                    logger.LogWarning("Timed out waiting for approval");
                    return ["Order Rejected - Timed Out waiting for approval"];
                }
            }
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

        [Function(nameof(RequestApproval))]
        public static void RequestApproval([ActivityTrigger] Guid orderId, 
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("RequestApproval");
            logger.LogInformation("Requesting Approval for {orderId}.", orderId);
        }

        [Function("NewOrderOrchestration_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            
            ILogger logger = executionContext.GetLogger("NewOrderOrchestration_HttpStart");
            var price = req.Query["price"] ?? "100";
            var inputData = new OrderOrchestrationInput
            {
                OrderId = Guid.NewGuid(),
                SeatNumbers = ["A1", "A2", "A3"],
                TotalPrice = Decimal.Parse(price)
            };
            
            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(NewOrderOrchestration), inputData);
            
            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return client.CreateCheckStatusResponse(req, instanceId);
        }

        [Function("NewOrderOrchestration_Approve")]
        public static async Task<HttpResponseData> Approve(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("NewOrderOrchestration_Approve");
            var approvalResult = await req.ReadFromJsonAsync<ApprovalResult>();
            var id = req.Query["id"];
            if (id == null || approvalResult == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            logger.LogInformation("Raising approval {approved} to {id}", approvalResult.IsApproved, id);
            await client.RaiseEventAsync(id, OrderApprovalEventName, approvalResult);
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
