using TodoApp.Models;

namespace TodoApp.Repositories
{
    public interface ITodoRepository
    {
        Task<List<TodoItem>> GetAllAsync(); // Gets all todo items
        Task<TodoItem?> GetByIdAsync(string id); // Gets one todo by id
        Task CreateAsync(TodoItem todoItem); // Creates a todo
        Task UpdateAsync(string id, TodoItem todoItem); // Updates a todo
        Task DeleteAsync(string id); // Deletes a todo
        Task ToggleCompleteAsync(string id); // Toggles completed status
    }
}