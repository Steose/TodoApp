using Microsoft.AspNetCore.Mvc;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp.Controllers
{
    public class TodoController : Controller
    {
        private readonly ITodoService _todoService;

        public TodoController(ITodoService todoService)
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

        public IActionResult Item()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Item(TodoItem todoItem)
        {
            if (!ModelState.IsValid)
            {
                return View(todoItem);
            }

            await _todoService.CreateAsync(todoItem);

            ViewBag.Message = "Todo item added successfully!";
            return View();
        }
    }
}