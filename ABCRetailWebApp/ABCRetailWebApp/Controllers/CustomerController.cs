using Microsoft.AspNetCore.Mvc;
using ABCRetailWebApp.Models;
using ABCRetailWebApp.Services;

namespace ABCRetailWebApp.Controllers
{ 
    public class CustomersController : Controller
    {
        private readonly TableStorageService _tableService;
        private readonly QueueStorageService _queueService;

        public CustomersController(TableStorageService tableService, QueueStorageService queueService)
        {
            _tableService = tableService;
            _queueService = queueService;
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            var customers = await _tableService.GetAllCustomers();
            return View(customers);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerProfile customer)
        {
            if (ModelState.IsValid)
            {
                customer.RowKey = Guid.NewGuid().ToString();
                customer.PartitionKey = "Customer";

                await _tableService.AddCustomerProfile(customer);

                // Add message to queue
                await _queueService.SendOrderMessageAsync($"New customer profile created: {customer.CustomerName}");

                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var customer = await _tableService.GetCustomer(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _tableService.DeleteCustomer(partitionKey, rowKey);
            await _queueService.SendOrderMessageAsync($"Customer profile deleted: {rowKey}");
            return RedirectToAction(nameof(Index));
        }
    }
}
