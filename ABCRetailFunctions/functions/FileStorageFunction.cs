using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Files.Shares;
using System.Net;
using System.Text.Json;
using System.Text;
using System.Web;

namespace ABCRetailFunctions.Functions
{
    public class FileStorageFunction
    {
        private readonly ILogger _logger;
        private readonly ShareClient _shareClient;
        private readonly ShareDirectoryClient _directoryClient;

        public FileStorageFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FileStorageFunction>();

            var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
            _shareClient = new ShareClient(connectionString, "contracts");

            _shareClient.CreateIfNotExists();
            _directoryClient = _shareClient.GetRootDirectoryClient();
        }

        [Function("UploadContractFromBase64")]
        public async Task<HttpResponseData> UploadContractFromBase64(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing base64 file upload to Azure Files");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var uploadRequest = JsonSerializer.Deserialize<FileUploadRequest>(requestBody);

                if (uploadRequest == null || string.IsNullOrEmpty(uploadRequest.Base64Content))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid file data");
                    return badResponse;
                }

                // Remove data URI prefix if present
                var base64Data = uploadRequest.Base64Content;
                if (base64Data.Contains(","))
                {
                    base64Data = base64Data.Split(',')[1];
                }

                // Convert base64 to byte array
                var fileBytes = Convert.FromBase64String(base64Data);

                // Generate file name
                var fileName = string.IsNullOrEmpty(uploadRequest.FileName)
                    ? $"contract_{Guid.NewGuid():N}.pdf"
                    : uploadRequest.FileName;

                var fileClient = _directoryClient.GetFileClient(fileName);

                // Create and upload file
                using (var stream = new MemoryStream(fileBytes))
                {
                    await fileClient.CreateAsync(stream.Length);
                    await fileClient.UploadRangeAsync(new Azure.HttpRange(0, stream.Length), stream);
                }

                _logger.LogInformation($"Contract uploaded successfully: {fileName}");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "Contract uploaded successfully to Azure Files",
                    fileName = fileName,
                    size = fileBytes.Length
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading contract: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetAllFiles")]
        public async Task<HttpResponseData> GetAllFiles(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all files from Azure Files");

            try
            {
                var files = new List<object>();

                await foreach (var item in _directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                    {
                        var fileClient = _directoryClient.GetFileClient(item.Name);
                        var properties = await fileClient.GetPropertiesAsync();

                        files.Add(new
                        {
                            name = item.Name,
                            size = properties.Value.ContentLength,
                            lastModified = properties.Value.LastModified,
                            contentType = properties.Value.ContentType
                        });
                    }
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    shareName = "contracts",
                    fileCount = files.Count,
                    files = files
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting files: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("DeleteFile")]
        public async Task<HttpResponseData> DeleteFile(
            [HttpTrigger(AuthorizationLevel.Function, "delete")] HttpRequestData req)
        {
            _logger.LogInformation("Processing file deletion from Azure Files");

            try
            {
                var query = HttpUtility.ParseQueryString(req.Url.Query);
                var fileName = query["fileName"];

                if (string.IsNullOrEmpty(fileName))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("fileName parameter is required");
                    return badResponse;
                }

                var fileClient = _directoryClient.GetFileClient(fileName);

                // Check if file exists
                var exists = await fileClient.ExistsAsync();
                if (!exists.Value)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync($"File '{fileName}' not found");
                    return notFoundResponse;
                }

                // Delete file
                await fileClient.DeleteAsync();

                _logger.LogInformation($"File deleted successfully: {fileName}");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "File deleted successfully",
                    fileName = fileName
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting file: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("DownloadFile")]
        public async Task<HttpResponseData> DownloadFile(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "DownloadFile/{fileName}")] HttpRequestData req,
            string fileName)
        {
            _logger.LogInformation($"Downloading file: {fileName}");

            try
            {
                var fileClient = _directoryClient.GetFileClient(fileName);

                // Check if file exists
                var exists = await fileClient.ExistsAsync();
                if (!exists.Value)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync($"File '{fileName}' not found");
                    return notFoundResponse;
                }

                // Download file
                var download = await fileClient.DownloadAsync();
                var properties = await fileClient.GetPropertiesAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", properties.Value.ContentType ?? "application/octet-stream");
                response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");

                await download.Value.Content.CopyToAsync(response.Body);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading file: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }

    public class FileUploadRequest
    {
        public string Base64Content { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? Description { get; set; }
    }
}