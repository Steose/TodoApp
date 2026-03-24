using Azure.Cosmos;
using Azure.Identity;
using Microsoft.Extensions.Options;
using TodoApp.Models;

namespace TodoApp.Repositories
{
    public class TodoRepository : ITodoRepository
    {
        private readonly CosmosContainer _container;

        public TodoRepository(IOptions<TodoDatabaseSettings> todoDatabaseSettings)
        {
            var settings = todoDatabaseSettings.Value;
            
            if (string.IsNullOrEmpty(settings.CosmosEndpoint) || string.IsNullOrEmpty(settings.CosmosKey))
            {
                throw new InvalidOperationException("CosmosDB endpoint and key must be configured.");
            }

            if (string.IsNullOrEmpty(settings.DatabaseName) || string.IsNullOrEmpty(settings.ContainerName))
            {
                throw new InvalidOperationException("Database name and container name must be configured.");
            }

            // Initialize CosmosDB client with key-based authentication
            var cosmosClient = new CosmosClient(settings.CosmosEndpoint, settings.CosmosKey);
            
            // Get reference to the database and container
            var database = cosmosClient.GetDatabase(settings.DatabaseName);
            _container = database.GetContainer(settings.ContainerName);
        }

        public async Task<List<TodoItem>> GetAllAsync()
        {
            try
            {
                var items = new List<TodoItem>();
                var query = _container.GetItemQueryIterator<TodoItem>(
                    new QueryDefinition("SELECT * FROM c ORDER BY c.CreatedAt DESC"));

                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    items.AddRange(response);
                }

                return items;
            }
            catch (CosmosOperationFailedException ex)
            {
                Console.Error.WriteLine($"CosmosDB operation error in GetAllAsync: {ex.Message}");
                return new List<TodoItem>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error in GetAllAsync: {ex.Message}");
                return new List<TodoItem>();
            }
        }

        public async Task<TodoItem?> GetByIdAsync(string id)
        {
            try
            {
                var response = await _container.ReadItemAsync<TodoItem>(id, new PartitionKey("TodoItem"));
                return response.Value;
            }
            catch (CosmosOperationFailedException ex) when (ex.Status == 404)
            {
                // Item not found
                return null;
            }
            catch (CosmosOperationFailedException ex)
            {
                Console.Error.WriteLine($"CosmosDB operation error in GetByIdAsync: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error in GetByIdAsync: {ex.Message}");
                return null;
            }
        }

        public async Task CreateAsync(TodoItem newTodo)
        {
            try
            {
                newTodo.PartitionKey = "TodoItem";
                if (string.IsNullOrEmpty(newTodo.Id))
                {
                    newTodo.Id = Guid.NewGuid().ToString();
                }

                await _container.CreateItemAsync(newTodo, new PartitionKey("TodoItem"));
            }
            catch (CosmosOperationFailedException ex)
            {
                Console.Error.WriteLine($"CosmosDB operation error in CreateAsync: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error in CreateAsync: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateAsync(string id, TodoItem updatedTodo)
        {
            try
            {
                updatedTodo.Id = id;
                updatedTodo.PartitionKey = "TodoItem";

                await _container.ReplaceItemAsync(updatedTodo, id, new PartitionKey("TodoItem"));
            }
            catch (CosmosOperationFailedException ex)
            {
                Console.Error.WriteLine($"CosmosDB operation error in UpdateAsync: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error in UpdateAsync: {ex.Message}");
                throw;
            }
        }

        public async Task RemoveAsync(string id)
        {
            try
            {
                await _container.DeleteItemAsync<TodoItem>(id, new PartitionKey("TodoItem"));
            }
            catch (CosmosOperationFailedException ex) when (ex.Status == 404)
            {
                // Item doesn't exist, which is fine for delete operation
                return;
            }
            catch (CosmosOperationFailedException ex)
            {
                Console.Error.WriteLine($"CosmosDB operation error in RemoveAsync: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error in RemoveAsync: {ex.Message}");
                throw;
            }
        }
    }
}
