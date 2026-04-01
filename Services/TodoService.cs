using TodoApp.Models; // TodoItem model
using TodoApp.Repositories; // Repository interface

namespace TodoApp.Services
{
    public class TodoService : ITodoService
    {
        private readonly ITodoRepository _todoRepository; // Repository dependency

        public TodoService(ITodoRepository todoRepository)
        {
            _todoRepository = todoRepository;
        }

        public async Task<List<TodoItem>> GetAsync()
        {
            // Return all todos from repository
            return await _todoRepository.GetAllAsync();
        }

        public async Task<TodoItem?> GetAsync(string id)
        {
            // Return one todo from repository
            return await _todoRepository.GetByIdAsync(id);
        }

        public async Task CreateAsync(TodoItem todoItem)
        {
            // Pass create request to repository
            await _todoRepository.CreateAsync(todoItem);
        }

        public async Task UpdateAsync(string id, TodoItem todoItem)
        {
            // Pass update request to repository
            await _todoRepository.UpdateAsync(id, todoItem);
        }

        public async Task RemoveAsync(string id)
        {
            // Pass delete request to repository
            await _todoRepository.DeleteAsync(id);
        }

        public async Task ToggleCompleteAsync(string id)
        {
            // Pass toggle request to repository
            await _todoRepository.ToggleCompleteAsync(id);
        }
    }
}