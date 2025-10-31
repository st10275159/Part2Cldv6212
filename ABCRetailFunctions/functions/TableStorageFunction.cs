using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System.Net;
using System.Text.Json;

namespace ABCRetailFunctions.Functions
{
    public class TableStorageFunction
    {
        private readonly ILogger _logger;
        private readonly TableClient _customerTableClient;
        private readonly TableClient _productTableClient;

        public TableStorageFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TableStorageFunction>();

            // Get connection string from environment variables
            var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");

            // Initialize table clients
            _customerTableClient = new TableClient(connectionString, "CustomerProfiles");
            _productTableClient = new TableClient(connectionString, "ProductInformation");

            // Create tables if they don't exist
            _customerTableClient.CreateIfNotExists();
            _productTableClient.CreateIfNotExists();
        }

        [Function("AddCustomerToTable")]
        public async Task<HttpResponseData> AddCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing request to add customer to table storage");

            try
            {
                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var customer = JsonSerializer.Deserialize<CustomerProfile>(requestBody);

                if (customer == null)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid customer data");
                    return badResponse;
                }

                // Set partition and row keys
                customer.PartitionKey = "Customer";
                customer.RowKey = Guid.NewGuid().ToString();

                // Add to table
                await _customerTableClient.AddEntityAsync(customer);

                _logger.LogInformation($"Customer added successfully: {customer.CustomerName}");

                // Create success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "Customer added successfully",
                    customerId = customer.RowKey,
                    customerName = customer.CustomerName
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding customer: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("AddProductToTable")]
        public async Task<HttpResponseData> AddProduct(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing request to add product to table storage");

            try
            {
                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var product = JsonSerializer.Deserialize<ProductInfo>(requestBody);

                if (product == null)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid product data");
                    return badResponse;
                }

                // Set partition and row keys
                product.PartitionKey = "Product";
                product.RowKey = Guid.NewGuid().ToString();

                // Add to table
                await _productTableClient.AddEntityAsync(product);

                _logger.LogInformation($"Product added successfully: {product.ProductName}");

                // Create success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "Product added successfully",
                    productId = product.RowKey,
                    productName = product.ProductName
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding product: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetAllCustomers")]
        public async Task<HttpResponseData> GetAllCustomers(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all customers from table storage");

            try
            {
                var customers = new List<CustomerProfile>();

                await foreach (var customer in _customerTableClient.QueryAsync<CustomerProfile>())
                {
                    customers.Add(customer);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(customers);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting customers: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }

    // Model classes
    public class CustomerProfile : Azure.Data.Tables.ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class ProductInfo : Azure.Data.Tables.ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StockQuantity { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}