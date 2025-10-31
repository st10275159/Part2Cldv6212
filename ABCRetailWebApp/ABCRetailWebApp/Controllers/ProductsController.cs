using Microsoft.AspNetCore.Mvc;
using ABCRetailWebApp.Models;
using ABCRetailWebApp.Services;

namespace ABCRetailWebApp.Controllers
{
    public class ProductsController : Controller
    {
        private readonly TableStorageService _tableService;
        private readonly QueueStorageService _queueService;

        public ProductsController(TableStorageService tableService, QueueStorageService queueService)
        {
            _tableService = tableService;
            _queueService = queueService;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var products = await _tableService.GetAllProducts();
            return View(products);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductInfo product)
        {
            if (ModelState.IsValid)
            {
                product.RowKey = Guid.NewGuid().ToString();
                product.PartitionKey = "Product";

                await _tableService.AddProduct(product);

                // Add message to queue
                await _queueService.SendInventoryMessageAsync($"New product added: {product.ProductName}, Quantity: {product.StockQuantity}");

                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var product = await _tableService.GetProduct(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var product = await _tableService.GetProduct(partitionKey, rowKey);
            await _tableService.DeleteProduct(partitionKey, rowKey);
            await _queueService.SendInventoryMessageAsync($"Product removed: {product?.ProductName}");
            return RedirectToAction(nameof(Index));
        }
    }
}
