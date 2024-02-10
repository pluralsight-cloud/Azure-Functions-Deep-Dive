using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Pluralsight.AzureFuncs
{
    public class OnNewBlob
    {
        private readonly ILogger<OnNewBlob> _logger;

        public OnNewBlob(ILogger<OnNewBlob> logger)
        {
            _logger = logger;
        }

        [Function(nameof(OnNewBlob))]
        public async Task Run([BlobTrigger("tickets/{name}", Connection = "AzureWebJobsStorage")] 
            Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            _logger.LogInformation("C# Blob trigger function Processed blob\n" +
                $" Name: {name} \n Data: {content}");
        }

        [Function(nameof(OnNewBlob2))]
        public async Task OnNewBlob2([BlobTrigger("tickets2/{name}", Connection = "AzureWebJobsStorage")] 
            BlobClient blobClient, string name)
        {
            var content = (await blobClient.DownloadContentAsync()).Value.Content.ToString();
            var props = await blobClient.GetPropertiesAsync();
            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name} \n" +
                $"Data: {content} \n" +
                $"{props.Value.LastModified} {props.Value.ContentLength} {props.Value.ContentType}");
        }
    }
}
