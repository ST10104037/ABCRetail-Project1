using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace ABCRetail.Services
{
    // Handles sending and reading order/inventory processing messages via Azure Queue Storage.
    public class QueueStorageService
    {
        private readonly QueueClient _queueClient;

        public QueueStorageService(IConfiguration config)
        {
            var connectionString = config["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Storage connection string is missing.");
            var queueName = config["AzureStorage:QueueName"] ?? "order-processing-queue";

            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists();
        }

        // message example: "Processing order #1023 - imageName: shoe123.jpg"
        public async Task SendMessageAsync(string message)
        {
            // Azure Queue messages must be Base64 or plain UTF-8; the SDK handles encoding for us.
            await _queueClient.SendMessageAsync(message);
        }

        public async Task<List<PeekedMessage>> PeekMessagesAsync(int maxMessages = 10)
        {
            var response = await _queueClient.PeekMessagesAsync(maxMessages);
            return response.Value.ToList();
        }

        public async Task<QueueMessage?> ReceiveAndDeleteNextMessageAsync()
        {
            var response = await _queueClient.ReceiveMessageAsync();
            var message = response.Value;
            if (message != null)
            {
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
            }
            return message;
        }
    }
}
