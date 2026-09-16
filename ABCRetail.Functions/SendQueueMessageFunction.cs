using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Functions
{
    // POST /api/SendQueueMessage
    // { "message": "Processing order #1023 - imageName: shoe123.jpg" }
    public class SendQueueMessageFunction
    {
        private readonly ILogger _logger;

        public SendQueueMessageFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SendQueueMessageFunction>();
        }

        [Function("SendQueueMessage")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "SendQueueMessage")] HttpRequestData req)
        {
            _logger.LogInformation("SendQueueMessage function triggered.");

            var body = await new StreamReader(req.Body).ReadToEndAsync();

            SendQueueMessageRequest? payload;
            try
            {
                payload = JsonSerializer.Deserialize<SendQueueMessageRequest>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse request body: {body}", body);
                var badJson = req.CreateResponse(HttpStatusCode.BadRequest);
                await badJson.WriteStringAsync("Request body is missing or is not valid JSON. Make sure Content-Type is application/json and the body is non-empty.");
                return badJson;
            }

            if (payload is null || string.IsNullOrWhiteSpace(payload.Message))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("A non-empty 'message' field is required.");
                return bad;
            }

            var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection")
                ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
            var queueName = Environment.GetEnvironmentVariable("QueueName") ?? "order-processing-queue";

            var queueClient = new QueueClient(connectionString, queueName);
            await queueClient.CreateIfNotExistsAsync();
            var result = await queueClient.SendMessageAsync(payload.Message);

            _logger.LogInformation("Sent message {messageId} to queue {queueName}", result.Value.MessageId, queueName);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new { messageId = result.Value.MessageId });
            return response;
        }
    }

    public class SendQueueMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}