using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Pluralsight.AzureFuncs
{
    public class NewPurchaseWebhook
    {
        private readonly ILogger _logger;

        public NewPurchaseWebhook(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<NewPurchaseWebhook>();
        }

        [Function(nameof(NewPurchaseWebhook))]
        public HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route="purchase")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            var name = req.Query.Get("name") ?? "Anonymous";
            response.WriteString($"Welcome {name}!");

            return response;
        }
    }
}
