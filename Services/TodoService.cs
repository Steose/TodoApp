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

        public async Task<List<TodoItem>> GetAsync() =>
            await _todoCollection.Find(_ => true).SortByDescending(x => x.CreatedAt).ToListAsync();

        public async Task<TodoItem?> GetAsync(string id) =>
            await _todoCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(TodoItem newTodo) =>
            await _todoCollection.InsertOneAsync(newTodo);

        public async Task UpdateAsync(string id, TodoItem updatedTodo) =>
            await _todoCollection.ReplaceOneAsync(x => x.Id == id, updatedTodo);

        public async Task RemoveAsync(string id) =>
            await _todoCollection.DeleteOneAsync(x => x.Id == id);

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