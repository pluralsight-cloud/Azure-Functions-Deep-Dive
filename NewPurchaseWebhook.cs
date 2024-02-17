using System.Net;
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
            [HttpTrigger(AuthorizationLevel.Function, "get", Route="purchase")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            var userAgent = req.Headers.GetValues("User-Agent").FirstOrDefault() ?? "Unknown";
            var name = req.Query.Get("name") ?? "Anonymous";
            response.WriteString($"Welcome {name} using {userAgent}!");

            return response;
        }
    }
}
