using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Pluralsight.AzureFuncs
{
    public class NewPurchaseWebhookResponse
    {
        [QueueOutput("neworders", Connection = "AzureWebJobsStorage")]
        public NewOrderMessage? Message { get; set; }
        public HttpResponseData? HttpResponse { get; set; }
    }


    public class NewPurchaseWebhook
    {
        private readonly ILogger _logger;

        public NewPurchaseWebhook(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<NewPurchaseWebhook>();
        }

        record NewOrderWebhook(int productId, int quantity, 
            string customerName, string customerEmail, decimal purchasePrice);

        [Function(nameof(NewPurchaseWebhook))]
        public async Task<NewPurchaseWebhookResponse> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route="purchase")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            //var body = await req.ReadAsStringAsync();
            var order = await req.ReadFromJsonAsync<NewOrderWebhook>();
            if (order == null) throw new ArgumentException("body was not deserializable as NewOrderWebhook");

            var message = new NewOrderMessage(
                    Guid.NewGuid(), // order id
	                order.productId, 
	                order.quantity,
	                order.customerName, 
	                order.customerEmail, 
                order.purchasePrice);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString($"{order.customerName} purchased product {order.productId}!");

            return new NewPurchaseWebhookResponse
            {
                Message = message,
                HttpResponse = response
            };
        }

        [Function(nameof(GetPurchase))]
        public HttpResponseData GetPurchase(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route="purchase/{orderId:guid}")] HttpRequestData req,
            [BlobInput("tickets/{orderId}.txt", Connection = "AzureWebJobsStorage")] BlobClient ticketClient,
            Guid orderId)
        {
            _logger.LogInformation($"Requested details of {orderId}");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            try
            {
                var ticketContents = ticketClient.DownloadContent().Value.Content.ToString();
                response.WriteString(ticketContents);
            }
            catch (RequestFailedException rfe) when (rfe.ErrorCode == "BlobNotFound")
            {
                _logger.LogError(rfe, $"Order {orderId} does not exist");
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            return response;
        }
    }
}
