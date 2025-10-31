using Azure.Data.Tables;
using ABCRetailWebApp.Models;

namespace ABCRetailWebApp.Services
{
    public class TableStorageService
    {
        private readonly TableClient _customerTableClient;
        private readonly TableClient _productTableClient;

        public TableStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];

            // Create table clients
            _customerTableClient = new TableClient(connectionString, "CustomerProfiles");
            _productTableClient = new TableClient(connectionString, "ProductInformation");

            // Create tables if they don't exist
            _customerTableClient.CreateIfNotExists();
            _productTableClient.CreateIfNotExists();
        }

        // Customer Profile Methods
        public async Task AddCustomerProfile(CustomerProfile customer)
        {
            await _customerTableClient.AddEntityAsync(customer);
        }

        public async Task<List<CustomerProfile>> GetAllCustomers()
        {
            var customers = new List<CustomerProfile>();

            await foreach (var customer in _customerTableClient.QueryAsync<CustomerProfile>())
            {
                customers.Add(customer);
            }

            return customers;
        }

        public async Task<CustomerProfile?> GetCustomer(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _customerTableClient.GetEntityAsync<CustomerProfile>(partitionKey, rowKey);
                return response.Value;
            }
            catch
            {
                return null;
            }
        }

        public async Task DeleteCustomer(string partitionKey, string rowKey)
        {
            await _customerTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // Product Methods
        public async Task AddProduct(ProductInfo product)
        {
            await _productTableClient.AddEntityAsync(product);
        }

        public async Task<List<ProductInfo>> GetAllProducts()
        {
            var products = new List<ProductInfo>();

            await foreach (var product in _productTableClient.QueryAsync<ProductInfo>())
            {
                products.Add(product);
            }

            return products;
        }

        public async Task<ProductInfo?> GetProduct(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _productTableClient.GetEntityAsync<ProductInfo>(partitionKey, rowKey);
                return response.Value;
            }
            catch
            {
                return null;
            }
        }

        public async Task DeleteProduct(string partitionKey, string rowKey)
        {
            await _productTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}