using TodoApp.Models;

namespace TodoApp.Services
{
    public interface ITodoService
    {
        Task<List<TodoItem>> GetAsync(); // Gets all todos
        Task<TodoItem?> GetAsync(string id); // Gets one todo by id
        Task CreateAsync(TodoItem todoItem); // Creates a todo
        Task UpdateAsync(string id, TodoItem todoItem); // Updates a todo
        Task RemoveAsync(string id); // Deletes a todo
        Task ToggleCompleteAsync(string id); // Toggles completed state
    }
}