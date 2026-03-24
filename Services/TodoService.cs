using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TodoApp.Models;

namespace TodoApp.Services
{
    public class TodoService : ITodoService
    {
        private readonly IMongoCollection<TodoItem> _todoCollection;

        public TodoService(IOptions<TodoDatabaseSettings> todoDatabaseSettings)
        {
            var mongoClient = new MongoClient(todoDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(todoDatabaseSettings.Value.DatabaseName);

            _todoCollection = mongoDatabase.GetCollection<TodoItem>(
                todoDatabaseSettings.Value.TodoCollectionName);
        }

        public async Task<List<TodoItem>> GetAsync()
        {
            try
            {
                return await _todoCollection.Find(_ => true).SortByDescending(x => x.CreatedAt).ToListAsync();
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                // Log or handle as needed in production
                Console.Error.WriteLine($"MongoDB connection error in GetAsync: {ex.Message}");
                return new List<TodoItem>();
            }
        }

        public async Task<TodoItem?> GetAsync(string id)
        {
            try
            {
                return await _todoCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                Console.Error.WriteLine($"MongoDB connection error in GetAsync(id): {ex.Message}");
                return null;
            }
        }

        public async Task CreateAsync(TodoItem newTodo)
        {
            try
            {
                await _todoCollection.InsertOneAsync(newTodo);
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                Console.Error.WriteLine($"MongoDB connection error in CreateAsync: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateAsync(string id, TodoItem updatedTodo)
        {
            try
            {
                await _todoCollection.ReplaceOneAsync(x => x.Id == id, updatedTodo);
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                Console.Error.WriteLine($"MongoDB connection error in UpdateAsync: {ex.Message}");
                throw;
            }
        }

        public async Task RemoveAsync(string id)
        {
            try
            {
                await _todoCollection.DeleteOneAsync(x => x.Id == id);
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                Console.Error.WriteLine($"MongoDB connection error in RemoveAsync: {ex.Message}");
                throw;
            }
        }

        public async Task ToggleCompleteAsync(string id)
        {
            var todo = await GetAsync(id);

            if (todo is null)
                return;

            todo.IsCompleted = !todo.IsCompleted;

            await UpdateAsync(id, todo);
        }
    }
}