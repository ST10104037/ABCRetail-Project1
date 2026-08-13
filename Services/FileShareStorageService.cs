using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace ABCRetail.Services
{
    // Handles storing and listing log files in Azure File Storage.
    public class FileShareStorageService
    {
        private readonly ShareClient _shareClient;

        public FileShareStorageService(IConfiguration config)
        {
            var connectionString = config["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Storage connection string is missing.");
            var shareName = config["AzureStorage:FileShareName"] ?? "logfiles";

            _shareClient = new ShareClient(connectionString, shareName);
            _shareClient.CreateIfNotExists();
        }

        public async Task UploadLogFileAsync(Stream fileStream, string fileName)
        {
            var rootDir = _shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(fileName);

            await fileClient.CreateAsync(fileStream.Length);
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, fileStream.Length), fileStream);
        }

        public List<string> ListLogFiles()
        {
            var rootDir = _shareClient.GetRootDirectoryClient();
            var names = new List<string>();
            foreach (ShareFileItem item in rootDir.GetFilesAndDirectories())
            {
                if (!item.IsDirectory)
                {
                    names.Add(item.Name);
                }
            }
            return names;
        }

        public async Task<Stream> DownloadLogFileAsync(string fileName)
        {
            var rootDir = _shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(fileName);
            var download = await fileClient.DownloadAsync();
            return download.Value.Content;
        }
    }
}
