using Microsoft.AspNetCore.Mvc;
using ABCRetailWebApp.Services;

namespace ABCRetailWebApp.Controllers
{
    public class QueuesController : Controller
    {
        private readonly QueueStorageService _queueService;

        public QueuesController(QueueStorageService queueService)
        {
            _queueService = queueService;
        }

        // GET: Queues
        public async Task<IActionResult> Index()
        {
            ViewBag.OrderQueueCount = await _queueService.GetOrderQueueLengthAsync();
            ViewBag.InventoryQueueCount = await _queueService.GetInventoryQueueLengthAsync();
            return View();
        }

        // GET: Queues/OrderMessages
        public async Task<IActionResult> OrderMessages()
        {
            var messages = await _queueService.GetOrderMessagesAsync();
            ViewBag.QueueName = "Order Processing Queue";
            return View("Messages", messages);
        }

        // GET: Queues/InventoryMessages
        public async Task<IActionResult> InventoryMessages()
        {
            var messages = await _queueService.GetInventoryMessagesAsync();
            ViewBag.QueueName = "Inventory Management Queue";
            return View("Messages", messages);
        }

        // GET: Queues/SendMessage
        public IActionResult SendMessage()
        {
            return View();
        }

        // POST: Queues/SendOrderMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendOrderMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                ModelState.AddModelError("", "Message cannot be empty");
                return View("SendMessage");
            }

            try
            {
                await _queueService.SendOrderMessageAsync(message);
                TempData["SuccessMessage"] = "Order message sent successfully!";
                return RedirectToAction(nameof(OrderMessages));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error sending message: {ex.Message}");
                return View("SendMessage");
            }
        }

        // POST: Queues/SendInventoryMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInventoryMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                ModelState.AddModelError("", "Message cannot be empty");
                return View("SendMessage");
            }

            try
            {
                await _queueService.SendInventoryMessageAsync(message);
                TempData["SuccessMessage"] = "Inventory message sent successfully!";
                return RedirectToAction(nameof(InventoryMessages));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error sending message: {ex.Message}");
                return View("SendMessage");
            }
        }
    }
}
