using TodoApp.Models;

namespace TodoApp.Services
{
    public interface ITodoService
    {
        Task<List<TodoItem>> GetAsync();
        Task<TodoItem?> GetAsync(string id);
        Task CreateAsync(TodoItem newTodo);
        Task UpdateAsync(string id, TodoItem updatedTodo);
        Task RemoveAsync(string id);
        Task ToggleCompleteAsync(string id);
    }
}