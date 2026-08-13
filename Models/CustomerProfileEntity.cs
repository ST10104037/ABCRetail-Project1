using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Models
{
    // Represents a row in the "CustomerProfiles" Azure Table
    public class CustomerProfileEntity : ITableEntity
    {
        // PartitionKey groups related rows together (here we use a fixed value "Customer")
        public string PartitionKey { get; set; } = "Customer";

        // RowKey must be unique within a partition - we use the customer's own Id
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
    }
}
