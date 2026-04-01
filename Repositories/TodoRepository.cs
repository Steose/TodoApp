using Microsoft.Extensions.Options; // Gives access to Options pattern
using MongoDB.Driver; // MongoDB driver classes
using TodoApp.Models; // TodoItem model
using TodoApp.Options; // Option classes

namespace TodoApp.Repositories
{
    public class TodoRepository : ITodoRepository
    {
        private readonly IMongoCollection<TodoItem> _todoCollection; // Mongo collection for todos

        public TodoRepository(
            IOptions<DatabaseProviderOptions> providerOptions,
            IOptions<MongoDbOptions> mongoOptions,
            IOptions<CosmosMongoOptions> cosmosOptions)
        {
            // Read which provider should be used
            var selectedProvider = providerOptions.Value.Provider?.Trim();

            string connectionString;
            string databaseName;
            string collectionName;

            // Pick the correct config block based on Provider setting
            if (string.Equals(selectedProvider, "CosmosMongo", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = cosmosOptions.Value.ConnectionString;
                databaseName = cosmosOptions.Value.DatabaseName;
                collectionName = cosmosOptions.Value.TodoCollectionName;
            }
            else
            {
                connectionString = mongoOptions.Value.ConnectionString;
                databaseName = mongoOptions.Value.DatabaseName;
                collectionName = mongoOptions.Value.TodoCollectionName;
            }

            // Validate connection string
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Database connection string is missing.");
            }

            // Validate database name
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("Database name is missing.");
            }

            // Validate collection name
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                throw new InvalidOperationException("Todo collection name is missing.");
            }

            // Create Mongo client
            var client = new MongoClient(connectionString);

            // Get database
            var database = client.GetDatabase(databaseName);

            // Get collection
            _todoCollection = database.GetCollection<TodoItem>(collectionName);
        }

        public async Task<List<TodoItem>> GetAllAsync()
        {
            // Returns all todo items sorted by newest first
            return await _todoCollection
                .Find(_ => true)
                .SortByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<TodoItem?> GetByIdAsync(string id)
        {
            // Find a todo by its Mongo document Id
            return await _todoCollection
                .Find(t => t.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task CreateAsync(TodoItem todoItem)
        {
            // Make sure system values are always set
            todoItem.Id = null;
            todoItem.CreatedAt = DateTime.UtcNow;
            todoItem.PartitionKey = "TodoItem";

            // Insert into database
            await _todoCollection.InsertOneAsync(todoItem);
        }

        public async Task UpdateAsync(string id, TodoItem todoItem)
        {
            // Keep safe values before updating
            var existing = await GetByIdAsync(id);

            if (existing == null)
            {
                return;
            }

            todoItem.Id = existing.Id; // Keep original Id
            todoItem.CreatedAt = existing.CreatedAt; // Keep original CreatedAt
            todoItem.PartitionKey = existing.PartitionKey; // Keep original partition key

            // Replace the full document
            await _todoCollection.ReplaceOneAsync(t => t.Id == id, todoItem);
        }

        public async Task DeleteAsync(string id)
        {
            // Delete matching item
            await _todoCollection.DeleteOneAsync(t => t.Id == id);
        }

        public async Task ToggleCompleteAsync(string id)
        {
            // Load existing item
            var existing = await GetByIdAsync(id);

            if (existing == null)
            {
                return;
            }

            // Toggle value
            var updatedValue = !existing.IsCompleted;

            // Update only the IsCompleted field
            var updateDefinition = Builders<TodoItem>.Update.Set(t => t.IsCompleted, updatedValue);

            await _todoCollection.UpdateOneAsync(t => t.Id == id, updateDefinition);
        }
    }
}