using System;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Pluralsight.AzureFuncs
{
    public class CreateNightlyReport
    {
        private readonly ILogger _logger;

        public CreateNightlyReport(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CreateNightlyReport>();
        }

        [Function("CreateNightlyReport")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer,
            [BlobInput("tickets", Connection ="AzureWebJobsStorage")] BlobContainerClient ticketsClient)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            await foreach(var f in ticketsClient.GetBlobsAsync())
            {
                var blob = ticketsClient.GetBlobClient(f.Name);
                var props = await blob.GetPropertiesAsync();
                if (DateTime.Now > props.Value.CreatedOn.AddDays(1))
                {
                    await blob.DeleteAsync();
                }
                else
                {
                    _logger.LogInformation($"Received order {f.Name}");
                }
            }

        }
    }
}
