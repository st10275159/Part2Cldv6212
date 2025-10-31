using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace ABCRetailWebApp.Services
{
    public class FileStorageService
    {
        private readonly ShareClient _shareClient;
        private readonly ShareDirectoryClient _directoryClient;
        private readonly string _shareName = "contracts";

        public FileStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            _shareClient = new ShareClient(connectionString, _shareName);

            // Create share if it doesn't exist
            _shareClient.CreateIfNotExists();

            // Get root directory
            _directoryClient = _shareClient.GetRootDirectoryClient();
        }

        // Upload file
        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty");
            }

            // Generate unique file name
            var fileName = Path.GetFileNameWithoutExtension(file.FileName)
                          + "_" + Guid.NewGuid().ToString().Substring(0, 8)
                          + Path.GetExtension(file.FileName);

            var fileClient = _directoryClient.GetFileClient(fileName);

            // Create and upload file
            using (var stream = file.OpenReadStream())
            {
                await fileClient.CreateAsync(stream.Length);
                await fileClient.UploadAsync(stream);
            }

            return fileName;
        }

        // Get all files
        public async Task<List<Models.FileInfo>> GetAllFilesAsync()
        {
            var files = new List<Models.FileInfo>();

            await foreach (var item in _directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                {
                    var fileClient = _directoryClient.GetFileClient(item.Name);
                    var properties = await fileClient.GetPropertiesAsync();

                    files.Add(new Models.FileInfo
                    {
                        Name = item.Name,
                        Size = properties.Value.ContentLength
                    });
                }
            }

            return files;
        }

        // Download file
        public async Task<(Stream, string)> DownloadFileAsync(string fileName)
        {
            var fileClient = _directoryClient.GetFileClient(fileName);
            var download = await fileClient.DownloadAsync();

            return (download.Value.Content, fileName);
        }

        // Delete file
        public async Task DeleteFileAsync(string fileName)
        {
            var fileClient = _directoryClient.GetFileClient(fileName);
            await fileClient.DeleteIfExistsAsync();
        }

        // Check if file exists
        public async Task<bool> FileExistsAsync(string fileName)
        {
            var fileClient = _directoryClient.GetFileClient(fileName);
            return await fileClient.ExistsAsync();
        }

        // Get file size
        public async Task<long> GetFileSizeAsync(string fileName)
        {
            var fileClient = _directoryClient.GetFileClient(fileName);
            var properties = await fileClient.GetPropertiesAsync();
            return properties.Value.ContentLength;
        }
    }
}