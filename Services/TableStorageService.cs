using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Services
{
    // Generic helper for working with Azure Table Storage entities.
    public class TableStorageService
    {
        private readonly string _connectionString;

        public TableStorageService(IConfiguration config)
        {
            _connectionString = config["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Storage connection string is missing.");
        }

        private TableClient GetTableClient(string tableName)
        {
            var client = new TableClient(_connectionString, tableName);
            client.CreateIfNotExists();
            return client;
        }

        public async Task AddEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new()
        {
            var table = GetTableClient(tableName);
            await table.AddEntityAsync(entity);
        }

        public async Task<List<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new()
        {
            var table = GetTableClient(tableName);
            var results = new List<T>();
            await foreach (var entity in table.QueryAsync<T>())
            {
                results.Add(entity);
            }
            return results;
        }

        public async Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            var table = GetTableClient(tableName);
            try
            {
                var response = await table.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task UpdateEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new()
        {
            var table = GetTableClient(tableName);
            await table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
        }

        public async Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey)
        {
            var table = GetTableClient(tableName);
            await table.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}
