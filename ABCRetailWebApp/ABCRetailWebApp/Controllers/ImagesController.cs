using Microsoft.AspNetCore.Mvc;
using ABCRetailWebApp.Models;
using ABCRetailWebApp.Services;

namespace ABCRetailWebApp.Controllers
{
    public class ImagesController : Controller
    {
        private readonly BlobStorageService _blobService;
        private readonly QueueStorageService _queueService;

        public ImagesController(BlobStorageService blobService, QueueStorageService queueService)
        {
            _blobService = blobService;
            _queueService = queueService;
        }

        // GET: Images
        public async Task<IActionResult> Index()
        {
            var blobs = await _blobService.GetAllBlobsAsync();
            return View(blobs);
        }

        // GET: Images/Upload
        public IActionResult Upload()
        {
            return View();
        }

        // POST: Images/Upload
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
                // Upload image to blob storage
                var imageUrl = await _blobService.UploadImageAsync(file);

                // Send message to queue
                await _queueService.SendOrderMessageAsync($"Uploaded image: {file.FileName}");

                TempData["SuccessMessage"] = $"Image uploaded successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error uploading image: {ex.Message}");
                return View();
            }
        }

        // GET: Images/Delete/blobname
        public async Task<IActionResult> Delete(string blobName)
        {
            if (string.IsNullOrEmpty(blobName))
            {
                return NotFound();
            }

            var blob = new BlobInfo
            {
                Name = blobName,
                Uri = _blobService.GetBlobUrl(blobName)
            };

            return View(blob);
        }

        // POST: Images/Delete/blobname
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string blobName)
        {
            try
            {
                await _blobService.DeleteBlobAsync(blobName);
                await _queueService.SendOrderMessageAsync($"Deleted image: {blobName}");

                TempData["SuccessMessage"] = "Image deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting image: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}