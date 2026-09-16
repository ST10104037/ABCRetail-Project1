using System.Net;
using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace ABCRetail.Functions
{
    // POST /api/UploadLogFile   (multipart/form-data, field name "file")
    // Returns: { "fileName": "..." }
    public class UploadLogFileFunction
    {
        private readonly ILogger _logger;

        public UploadLogFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadLogFileFunction>();
        }

        [Function("UploadLogFile")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "UploadLogFile")] HttpRequestData req)
        {
            _logger.LogInformation("UploadLogFile function triggered.");

            var contentTypeHeader = req.Headers.TryGetValues("Content-Type", out var values) ? values.First() : null;
            if (contentTypeHeader is null || !contentTypeHeader.Contains("multipart/form-data"))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Expected multipart/form-data with a 'file' field.");
                return bad;
            }

            var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentTypeHeader).Boundary).Value;
            var reader = new MultipartReader(boundary!, req.Body);

            string? fileName = null;
            using var memoryStream = new MemoryStream();

            MultipartSection? section;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var contentDisposition = section.GetContentDispositionHeader();
                if (contentDisposition != null && contentDisposition.IsFileDisposition())
                {
                    fileName = contentDisposition.FileName.Value ?? $"log_{Guid.NewGuid()}.txt";
                    await section.Body.CopyToAsync(memoryStream);
                }
            }

            if (fileName is null)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("No file field found in the request.");
                return bad;
            }

            memoryStream.Position = 0;

            var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection")
                ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
            var shareName = Environment.GetEnvironmentVariable("FileShareName") ?? "logfiles";

            var shareClient = new ShareClient(connectionString, shareName);
            await shareClient.CreateIfNotExistsAsync();
            var rootDir = shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(fileName);

            await fileClient.CreateAsync(memoryStream.Length);
            if (memoryStream.Length > 0)
            {
                await fileClient.UploadRangeAsync(new Azure.HttpRange(0, memoryStream.Length), memoryStream);
            }

            _logger.LogInformation("Uploaded log file {fileName} to share {shareName}", fileName, shareName);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new { fileName });
            return response;
        }
    }
}
