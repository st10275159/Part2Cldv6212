using Azure.Storage.Blobs;
using ABCRetailWebApp.Models;

namespace ABCRetailWebApp.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _containerClient;
        private readonly string _containerName = "product-images";

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            // Create container if it doesn't exist
            _containerClient.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
        }

        // Upload image to blob storage
        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty");
            }

            // Generate unique blob name
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = _containerClient.GetBlobClient(fileName);

            // Set content type
            var blobHttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = file.ContentType
            };

            // Upload file
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new Azure.Storage.Blobs.Models.BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders
                });
            }

            // Return the URL of the uploaded blob
            return blobClient.Uri.ToString();
        }

        // Get all blobs
        public async Task<List<ABCRetailWebApp.Models.BlobInfo>> GetAllBlobsAsync()
        {
            var blobs = new List<ABCRetailWebApp.Models.BlobInfo>();

            await foreach (var blobItem in _containerClient.GetBlobsAsync())
            {
                var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                var properties = await blobClient.GetPropertiesAsync();

                blobs.Add(new ABCRetailWebApp.Models.BlobInfo
                {
                    Name = blobItem.Name,
                    Uri = blobClient.Uri.ToString(),
                    ContentType = properties.Value.ContentType,
                    Size = properties.Value.ContentLength
                });
            }

            return blobs;
        }

        // Download blob
        public async Task<(Stream, string, string)> DownloadBlobAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var download = await blobClient.DownloadAsync();
            var properties = await blobClient.GetPropertiesAsync();

            return (download.Value.Content, properties.Value.ContentType, blobName);
        }

        // Delete blob
        public async Task DeleteBlobAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        // Get blob URL
        public string GetBlobUrl(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            return blobClient.Uri.ToString();
        }
    }
}
