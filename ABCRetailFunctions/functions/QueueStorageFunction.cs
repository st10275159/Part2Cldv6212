using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Net;
using System.Text.Json;

namespace ABCRetailFunctions.Functions
{
    public class QueueStorageFunction
    {
        private readonly ILogger _logger;
        private readonly QueueClient _orderQueueClient;
        private readonly QueueClient _inventoryQueueClient;

        public QueueStorageFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueStorageFunction>();

            var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");

            // Initialize queue clients
            _orderQueueClient = new QueueClient(connectionString, "order-processing");
            _inventoryQueueClient = new QueueClient(connectionString, "inventory-management");

            // Create queues if they don't exist
            _orderQueueClient.CreateIfNotExists();
            _inventoryQueueClient.CreateIfNotExists();
        }

        [Function("SendOrderMessage")]
        public async Task<HttpResponseData> SendOrderMessage(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing request to send order message to queue");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var messageRequest = JsonSerializer.Deserialize<QueueMessageRequest>(requestBody);

                if (messageRequest == null || string.IsNullOrEmpty(messageRequest.Message))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid message data");
                    return badResponse;
                }

                // Send message to queue
                var receipt = await _orderQueueClient.SendMessageAsync(messageRequest.Message);

                _logger.LogInformation($"Order message sent successfully: {messageRequest.Message}");

                // Create success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "Order message sent successfully",
                    messageId = receipt.Value.MessageId,
                    messageText = messageRequest.Message,
                    insertionTime = receipt.Value.InsertionTime
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending order message: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("SendInventoryMessage")]
        public async Task<HttpResponseData> SendInventoryMessage(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing request to send inventory message to queue");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var messageRequest = JsonSerializer.Deserialize<QueueMessageRequest>(requestBody);

                if (messageRequest == null || string.IsNullOrEmpty(messageRequest.Message))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid message data");
                    return badResponse;
                }

                // Send message to queue
                var receipt = await _inventoryQueueClient.SendMessageAsync(messageRequest.Message);

                _logger.LogInformation($"Inventory message sent successfully: {messageRequest.Message}");

                // Create success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "Inventory message sent successfully",
                    messageId = receipt.Value.MessageId,
                    messageText = messageRequest.Message,
                    insertionTime = receipt.Value.InsertionTime
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending inventory message: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("ReadOrderMessages")]
        public async Task<HttpResponseData> ReadOrderMessages(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Reading messages from order queue");

            try
            {
                var messages = new List<object>();

                // Peek at messages (doesn't remove them from queue)
                var peekedMessages = await _orderQueueClient.PeekMessagesAsync(maxMessages: 32);

                foreach (var message in peekedMessages.Value)
                {
                    messages.Add(new
                    {
                        messageId = message.MessageId,
                        messageText = message.MessageText,
                        insertedOn = message.InsertedOn,
                        dequeueCount = message.DequeueCount
                    });
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    queueName = "order-processing",
                    messageCount = messages.Count,
                    messages = messages
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading order messages: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("ReadInventoryMessages")]
        public async Task<HttpResponseData> ReadInventoryMessages(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Reading messages from inventory queue");

            try
            {
                var messages = new List<object>();

                // Peek at messages (doesn't remove them from queue)
                var peekedMessages = await _inventoryQueueClient.PeekMessagesAsync(maxMessages: 32);

                foreach (var message in peekedMessages.Value)
                {
                    messages.Add(new
                    {
                        messageId = message.MessageId,
                        messageText = message.MessageText,
                        insertedOn = message.InsertedOn,
                        dequeueCount = message.DequeueCount
                    });
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    queueName = "inventory-management",
                    messageCount = messages.Count,
                    messages = messages
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading inventory messages: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("ProcessOrderMessage")]
        public async Task<HttpResponseData> ProcessOrderMessage(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing (receiving and deleting) order message from queue");

            try
            {
                // Receive message (this makes it invisible to other consumers)
                var messages = await _orderQueueClient.ReceiveMessagesAsync(maxMessages: 1);

                if (messages.Value == null || messages.Value.Length == 0)
                {
                    var noMessageResponse = req.CreateResponse(HttpStatusCode.OK);
                    await noMessageResponse.WriteStringAsync("No messages in queue");
                    return noMessageResponse;
                }

                var message = messages.Value[0];

                // Process the message (simulate processing)
                _logger.LogInformation($"Processing message: {message.MessageText}");

                // Delete the message after processing
                await _orderQueueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "Message processed and deleted successfully",
                    messageId = message.MessageId,
                    messageText = message.MessageText
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing order message: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }

    public class QueueMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}