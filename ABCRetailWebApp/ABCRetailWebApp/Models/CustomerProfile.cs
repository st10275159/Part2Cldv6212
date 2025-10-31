using ABCRetailWebApp.Models;
using Azure;
using Azure.Data.Tables;

namespace ABCRetailWebApp.Models
{
    // Customer Profile for Table Storage
    public class CustomerProfile : ITableEntity
    {
        // Required by ITableEntity
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    // Product Information for Table Storage
    public class ProductInfo : ITableEntity
    {
        // Required by ITableEntity
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StockQuantity { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    // ViewModel for uploading images
    public class ImageUploadViewModel
    {
        public IFormFile ImageFile { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // ViewModel for file upload
    public class FileUploadViewModel
    {
        public IFormFile File { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // Model to display blob information
    public class BlobInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Uri { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    // Model to display file information
    public class FileInfo
    {
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    // Model for queue messages
    public class QueueMessage
    {
        public string MessageId { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTimeOffset InsertedOn { get; set; }
    }
}