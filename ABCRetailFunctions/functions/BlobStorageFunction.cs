using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Net;

namespace ABCRetailFunctions.Functions
{
    public class BlobStorageFunction
    {
        private readonly ILogger _logger;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _containerClient;

        public BlobStorageFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BlobStorageFunction>();

            var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = _blobServiceClient.GetBlobContainerClient("product-images");

            // Create container if it doesn't exist
            _containerClient.CreateIfNotExists(PublicAccessType.Blob);
        }

        // Use this function to upload an image via base64 JSON payload!
        [Function("UploadImageFromBase64")]
        public async Task<HttpResponseData> UploadImageFromBase64(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing base64 image upload to blob storage");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var uploadRequest = System.Text.Json.JsonSerializer.Deserialize<ImageUploadRequest>(requestBody);

                if (uploadRequest == null || string.IsNullOrEmpty(uploadRequest.Base64Image))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid image data");
                    return badResponse;
                }

                // Remove data URI prefix if present (e.g., "data:image/png;base64,")
                var base64Data = uploadRequest.Base64Image;
                if (base64Data.Contains(","))
                {
                    base64Data = base64Data.Split(',')[1];
                }

                // Convert base64 to byte array
                var imageBytes = Convert.FromBase64String(base64Data);

                // Generate unique blob name
                var fileName = Guid.NewGuid().ToString() + (uploadRequest.FileExtension ?? ".jpg");
                var blobClient = _containerClient.GetBlobClient(fileName);

                // Set content type
                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = uploadRequest.ContentType ?? "image/jpeg"
                };

                // Upload file
                using (var stream = new MemoryStream(imageBytes))
                {
                    await blobClient.UploadAsync(stream, new BlobUploadOptions
                    {
                        HttpHeaders = blobHttpHeaders
                    });
                }

                _logger.LogInformation($"Base64 image uploaded successfully: {fileName}");

                // Create success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "Image uploaded successfully",
                    fileName = fileName,
                    blobUrl = blobClient.Uri.ToString(),
                    size = imageBytes.Length
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading base64 image: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetAllBlobs")]
        public async Task<HttpResponseData> GetAllBlobs(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all blobs from storage");

            try
            {
                var blobs = new List<object>();

                await foreach (var blobItem in _containerClient.GetBlobsAsync())
                {
                    var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                    var properties = await blobClient.GetPropertiesAsync();

                    blobs.Add(new
                    {
                        name = blobItem.Name,
                        url = blobClient.Uri.ToString(),
                        contentType = properties.Value.ContentType,
                        size = properties.Value.ContentLength,
                        createdOn = properties.Value.CreatedOn
                    });
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(blobs);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting blobs: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }

    public class ImageUploadRequest
    {
        public string Base64Image { get; set; } = string.Empty;
        public string? FileExtension { get; set; }
        public string? ContentType { get; set; }
        public string? Description { get; set; }
    }
}