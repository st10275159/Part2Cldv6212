using Microsoft.AspNetCore.Mvc;
using ABCRetailWebApp.Services;

namespace ABCRetailWebApp.Controllers
{
    public class FilesController : Controller
    {
        private readonly FileStorageService _fileService;
        private readonly QueueStorageService _queueService;

        public FilesController(FileStorageService fileService, QueueStorageService queueService)
        {
            _fileService = fileService;
            _queueService = queueService;
        }

        // GET: Files
        public async Task<IActionResult> Index()
        {
            var files = await _fileService.GetAllFilesAsync();
            return View(files);
        }

        // GET: Files/Upload
        public IActionResult Upload()
        {
            return View();
        }

        // POST: Files/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file to upload");
                return View();
            }

            try
            {
                // Upload file to Azure Files
                var fileName = await _fileService.UploadFileAsync(file);

                // Send message to queue
                await _queueService.SendInventoryMessageAsync($"Uploaded contract: {fileName}");

                TempData["SuccessMessage"] = $"File uploaded successfully as: {fileName}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                return View();
            }
        }

        // GET: Files/Download/filename
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            try
            {
                var (stream, name) = await _fileService.DownloadFileAsync(fileName);

                // Send message to queue
                await _queueService.SendInventoryMessageAsync($"Downloaded contract: {fileName}");

                return File(stream, "application/octet-stream", name);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading file: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Files/Delete/filename
        public async Task<IActionResult> Delete(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            var fileExists = await _fileService.FileExistsAsync(fileName);
            if (!fileExists)
            {
                return NotFound();
            }

            var fileInfo = new Models.FileInfo
            {
                Name = fileName,
                Size = await _fileService.GetFileSizeAsync(fileName)
            };

            return View(fileInfo);
        }

        // POST: Files/Delete/filename
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string fileName)
        {
            try
            {
                await _fileService.DeleteFileAsync(fileName);
                await _queueService.SendInventoryMessageAsync($"Deleted contract: {fileName}");

                TempData["SuccessMessage"] = "File deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting file: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
