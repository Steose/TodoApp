using TodoApp.Models;

namespace TodoApp.Repositories
{
    public interface ITodoRepository
    {
        Task<List<TodoItem>> GetAllAsync();
        Task<TodoItem?> GetByIdAsync(string id);
        Task CreateAsync(TodoItem newTodo);
        Task UpdateAsync(string id, TodoItem updatedTodo);
        Task RemoveAsync(string id);
    }
}
