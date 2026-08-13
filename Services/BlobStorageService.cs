using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace ABCRetail.Services
{
    // Handles uploading, listing, and deleting images/multimedia in Azure Blob Storage.
    //
    // NOTE: Most storage accounts now have "Allow Blob public access" disabled by default
    // (and many student/org subscriptions block it via policy), so we create the container
    // as PRIVATE and generate short-lived SAS URLs on demand to display images instead of
    // relying on public/anonymous access.
    public class BlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration config)
        {
            var connectionString = config["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Storage connection string is missing.");
            var containerName = config["AzureStorage:BlobContainerName"] ?? "product-images";

            _containerClient = new BlobContainerClient(connectionString, containerName);

            // Private container - no anonymous/public access required.
            _containerClient.CreateIfNotExists(PublicAccessType.None);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            // Prefix with a GUID to avoid name clashes while keeping the original name readable
            var blobName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = _containerClient.GetBlobClient(blobName);

            var headers = new BlobHttpHeaders { ContentType = contentType };
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = headers });

            return blobName;
        }

        public List<(string Name, string Url)> ListBlobs()
        {
            var results = new List<(string, string)>();
            foreach (var blobItem in _containerClient.GetBlobs())
            {
                results.Add((blobItem.Name, GetBlobUrl(blobItem.Name)));
            }
            return results;
        }

        public async Task DeleteBlobAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        // Returns a URL usable by the browser for a short time (1 hour).
        // Falls back to the plain blob URI if SAS generation isn't possible
        // (e.g. when connecting via a credential that can't sign SAS tokens).
        public string GetBlobUrl(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _containerClient.Name,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                return blobClient.GenerateSasUri(sasBuilder).ToString();
            }

            return blobClient.Uri.ToString();
        }
    }
}