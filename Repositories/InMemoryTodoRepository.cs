using System.Collections.Concurrent;
using TodoApp.Models;

namespace TodoApp.Repositories
{
    public class InMemoryTodoRepository : ITodoRepository
    {
        private readonly ConcurrentDictionary<string, TodoItem> _items = new();

        public Task<List<TodoItem>> GetAllAsync()
        {
            var todos = _items.Values
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            return Task.FromResult(todos);
        }

        public Task<TodoItem?> GetByIdAsync(string id)
        {
            _items.TryGetValue(id, out var todo);
            return Task.FromResult(todo is null ? null : Clone(todo));
        }

        public Task CreateAsync(TodoItem todoItem)
        {
            var created = Clone(todoItem);
            created.Id = Guid.NewGuid().ToString("N");
            created.CreatedAt = DateTime.UtcNow;
            created.PartitionKey = "TodoItem";

            _items[created.Id] = created;
            return Task.CompletedTask;
        }

        public async Task UpdateAsync(string id, TodoItem todoItem)
        {
            var existing = await GetByIdAsync(id);
            if (existing is null)
            {
                return;
            }

            var updated = Clone(todoItem);
            updated.Id = existing.Id;
            updated.CreatedAt = existing.CreatedAt;
            updated.PartitionKey = existing.PartitionKey;

            _items[id] = updated;
        }

        public Task DeleteAsync(string id)
        {
            _items.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public async Task ToggleCompleteAsync(string id)
        {
            var existing = await GetByIdAsync(id);
            if (existing is null || existing.Id is null)
            {
                return;
            }

            existing.IsCompleted = !existing.IsCompleted;
            _items[id] = existing;
        }

        private static TodoItem Clone(TodoItem source)
        {
            return new TodoItem
            {
                Id = source.Id,
                Title = source.Title,
                Description = source.Description,
                IsCompleted = source.IsCompleted,
                CreatedAt = source.CreatedAt,
                PartitionKey = source.PartitionKey
            };
        }
    }
}
