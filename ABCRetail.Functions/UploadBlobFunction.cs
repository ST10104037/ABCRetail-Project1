using System.Net;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace ABCRetail.Functions
{
    // POST /api/UploadBlob   (multipart/form-data, field name "file")
    // Returns: { "blobName": "...", "url": "..." }
    public class UploadBlobFunction
    {
        private readonly ILogger _logger;

        public UploadBlobFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadBlobFunction>();
        }

        [Function("UploadBlob")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "UploadBlob")] HttpRequestData req)
        {
            _logger.LogInformation("UploadBlob function triggered.");

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
            string? contentType = null;
            using var memoryStream = new MemoryStream();

            MultipartSection? section;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var contentDisposition = section.GetContentDispositionHeader();
                if (contentDisposition != null && contentDisposition.IsFileDisposition())
                {
                    fileName = contentDisposition.FileName.Value ?? "upload.dat";
                    contentType = section.ContentType ?? "application/octet-stream";
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
            var containerName = Environment.GetEnvironmentVariable("BlobContainerName") ?? "product-images";

            var containerClient = new BlobContainerClient(connectionString, containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(memoryStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            });

            string url;
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);
                url = blobClient.GenerateSasUri(sasBuilder).ToString();
            }
            else
            {
                url = blobClient.Uri.ToString();
            }

            _logger.LogInformation("Uploaded blob {blobName} to container {containerName}", blobName, containerName);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new { blobName, url });
            return response;
        }
    }
}
