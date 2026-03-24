/* using Microsoft.AspNetCore.Mvc;
using TodoAppMvcMongo.Models;
using TodoAppMvcMongo.Services;

namespace TodoAppMvcMongo.Controllers
{
    public class TodoController : Controller
    {
        private readonly TodoService _todoService;

        public TodoController(TodoService todoService)
        {
            _todoService = todoService;
        }

        public async Task<IActionResult> Index()
        {
            var todos = await _todoService.GetAsync();
            return View(todos);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TodoItem todoItem)
        {
            if (!ModelState.IsValid)
                return View(todoItem);

            await _todoService.CreateAsync(todoItem);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var todo = await _todoService.GetAsync(id);

            if (todo == null)
                return NotFound();

            return View(todo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, TodoItem todoItem)
        {
            if (id != todoItem.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(todoItem);

            var existingTodo = await _todoService.GetAsync(id);
            if (existingTodo == null)
                return NotFound();

            await _todoService.UpdateAsync(id, todoItem);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            var todo = await _todoService.GetAsync(id);

            if (todo == null)
                return NotFound();

            return View(todo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var todo = await _todoService.GetAsync(id);

            if (todo == null)
                return NotFound();

            await _todoService.RemoveAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleComplete(string id)
        {
            await _todoService.ToggleCompleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
} */

using Microsoft.AspNetCore.Mvc;
namespace TodoApp.Controllers
{
    public class TodoController : Controller
    {
        public IActionResult Item()
        {
            return View();
        }
        [HttpPost]
    public IActionResult Item(string id, string title, string description,bool isCompleted)
    {
        // Add todo logic here
        // ...

        // Write to the console
        Console.WriteLine($"New todo added - ID: {id}: Title: {title}: Description: {description}: IsCompleted: {isCompleted}");

        // Send a message to the user
        return Content($"Added ID {id} {title} Todo with {description} description and {isCompleted} status");
    }
    }
}