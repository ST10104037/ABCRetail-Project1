using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ABCRetail.Functions
{
    // Automatically triggered whenever a new message lands on the queue - this is the
    // "reads from" half of the queue requirement (SendQueueMessageFunction is the "writes to" half).
    // Demonstrates a simple processing pipeline: queue message -> appended to a processing log
    // in Azure Files, showing how functions can be chained across storage services.
    public class ProcessQueueMessageFunction
    {
        private readonly ILogger _logger;

        public ProcessQueueMessageFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProcessQueueMessageFunction>();
        }

        [Function("ProcessQueueMessage")]
        public async Task Run(
            [QueueTrigger("%QueueName%", Connection = "AzureStorageConnection")] string queueMessage)
        {
            _logger.LogInformation("ProcessQueueMessage triggered with message: {message}", queueMessage);

            var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection")
                ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
            var shareName = Environment.GetEnvironmentVariable("FileShareName") ?? "logfiles";

            var shareClient = new ShareClient(connectionString, shareName);
            await shareClient.CreateIfNotExistsAsync();
            var rootDir = shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient("queue-processing-log.txt");

            var logLine = $"[{DateTimeOffset.UtcNow:u}] Processed queue message: {queueMessage}{Environment.NewLine}";

            byte[] combinedBytes;
            if (await fileClient.ExistsAsync())
            {
                var download = await fileClient.DownloadAsync();
                using var existingStream = new MemoryStream();
                await download.Value.Content.CopyToAsync(existingStream);
                var existingBytes = existingStream.ToArray();
                var newBytes = Encoding.UTF8.GetBytes(logLine);

                combinedBytes = new byte[existingBytes.Length + newBytes.Length];
                Buffer.BlockCopy(existingBytes, 0, combinedBytes, 0, existingBytes.Length);
                Buffer.BlockCopy(newBytes, 0, combinedBytes, existingBytes.Length, newBytes.Length);

                await fileClient.DeleteAsync();
            }
            else
            {
                combinedBytes = Encoding.UTF8.GetBytes(logLine);
            }

            await fileClient.CreateAsync(combinedBytes.Length);
            if (combinedBytes.Length > 0)
            {
                await fileClient.UploadRangeAsync(new Azure.HttpRange(0, combinedBytes.Length), new MemoryStream(combinedBytes));
            }

            _logger.LogInformation("Appended processing entry to queue-processing-log.txt");
        }
    }
}
