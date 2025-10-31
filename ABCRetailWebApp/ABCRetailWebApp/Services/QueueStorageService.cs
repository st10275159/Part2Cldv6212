using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using ABCRetailWebApp.Models;

namespace ABCRetailWebApp.Services
{
    public class QueueStorageService
    {
        private readonly QueueClient _orderQueueClient;
        private readonly QueueClient _inventoryQueueClient;

        public QueueStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];

            // Create queue clients
            _orderQueueClient = new QueueClient(connectionString, "order-processing");
            _inventoryQueueClient = new QueueClient(connectionString, "inventory-management");

            // Create queues if they don't exist
            _orderQueueClient.CreateIfNotExists();
            _inventoryQueueClient.CreateIfNotExists();
        }

        // Send message to order processing queue
        public async Task SendOrderMessageAsync(string message)
        {
            await _orderQueueClient.SendMessageAsync(message);
        }

        // Send message to inventory management queue
        public async Task SendInventoryMessageAsync(string message)
        {
            await _inventoryQueueClient.SendMessageAsync(message);
        }

        // Get messages from order processing queue
        public async Task<List<Models.QueueMessage>> GetOrderMessagesAsync(int maxMessages = 32)
        {
            var messages = new List<Models.QueueMessage>();

            // Peek at messages without removing them
            var peekedMessages = await _orderQueueClient.PeekMessagesAsync(maxMessages);

            foreach (var message in peekedMessages.Value)
            {
                messages.Add(new Models.QueueMessage
                {
                    MessageId = message.MessageId,
                    MessageText = message.MessageText,
                    InsertedOn = message.InsertedOn ?? DateTimeOffset.UtcNow
                });
            }

            return messages;
        }

        // Get messages from inventory management queue
        public async Task<List<Models.QueueMessage>> GetInventoryMessagesAsync(int maxMessages = 32)
        {
            var messages = new List<Models.QueueMessage>();

            // Peek at messages without removing them
            var peekedMessages = await _inventoryQueueClient.PeekMessagesAsync(maxMessages);

            foreach (var message in peekedMessages.Value)
            {
                messages.Add(new Models.QueueMessage
                {
                    MessageId = message.MessageId,
                    MessageText = message.MessageText,
                    InsertedOn = message.InsertedOn ?? DateTimeOffset.UtcNow
                });
            }

            return messages;
        }

        // Process and delete a message from order queue
        public async Task<Azure.Storage.Queues.Models.QueueMessage[]> ReceiveOrderMessagesAsync(int maxMessages = 10)

        {
            var response = await _orderQueueClient.ReceiveMessagesAsync(maxMessages);
            return response.Value;
        }

        // Delete message from order queue
        public async Task DeleteOrderMessageAsync(string messageId, string popReceipt)
        {
            await _orderQueueClient.DeleteMessageAsync(messageId, popReceipt);
        }

        // Get queue properties (message count)
        public async Task<int> GetOrderQueueLengthAsync()
        {
            var properties = await _orderQueueClient.GetPropertiesAsync();
            return properties.Value.ApproximateMessagesCount;
        }

        public async Task<int> GetInventoryQueueLengthAsync()
        {
            var properties = await _inventoryQueueClient.GetPropertiesAsync();
            return properties.Value.ApproximateMessagesCount;
        }
    }
}