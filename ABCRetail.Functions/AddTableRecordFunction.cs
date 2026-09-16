using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Functions
{
    // Request contract:
    // POST /api/AddTableRecord
    // {
    //   "tableName": "CustomerProfiles",
    //   "partitionKey": "Customer",
    //   "rowKey": "optional-guid",       // generated if omitted
    //   "properties": { "FullName": "Jane Doe", "Email": "jane@x.com", ... }
    // }
    public class AddTableRecordFunction
    {
        private readonly ILogger _logger;

        public AddTableRecordFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AddTableRecordFunction>();
        }

        [Function("AddTableRecord")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "AddTableRecord")] HttpRequestData req)
        {
            _logger.LogInformation("AddTableRecord function triggered.");

            var body = await new StreamReader(req.Body).ReadToEndAsync();

            AddTableRecordRequest? payload;
            try
            {
                payload = JsonSerializer.Deserialize<AddTableRecordRequest>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse request body: {body}", body);
                var badJson = req.CreateResponse(HttpStatusCode.BadRequest);
                await badJson.WriteStringAsync("Request body is missing or is not valid JSON. Make sure Content-Type is application/json and the body is non-empty.");
                return badJson;
            }

            if (payload is null || string.IsNullOrWhiteSpace(payload.TableName) || string.IsNullOrWhiteSpace(payload.PartitionKey))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("tableName and partitionKey are required.");
                return bad;
            }

            var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection")
                ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;

            var tableClient = new TableClient(connectionString, payload.TableName);
            await tableClient.CreateIfNotExistsAsync();

            var rowKey = string.IsNullOrWhiteSpace(payload.RowKey) ? Guid.NewGuid().ToString() : payload.RowKey;

            var entity = new TableEntity(payload.PartitionKey, rowKey);
            if (payload.Properties != null)
            {
                foreach (var kvp in payload.Properties)
                {
                    entity[kvp.Key] = kvp.Value.ToString();
                }
            }

            await tableClient.AddEntityAsync(entity);
            _logger.LogInformation("Added entity {partitionKey}/{rowKey} to table {tableName}",
                payload.PartitionKey, rowKey, payload.TableName);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new { partitionKey = payload.PartitionKey, rowKey });
            return response;
        }
    }

    public class AddTableRecordRequest
    {
        public string TableName { get; set; } = string.Empty;
        public string PartitionKey { get; set; } = string.Empty;
        public string? RowKey { get; set; }
        public Dictionary<string, JsonElement>? Properties { get; set; }
    }
}