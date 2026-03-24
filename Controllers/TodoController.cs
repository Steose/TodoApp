using Microsoft.AspNetCore.Mvc;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp.Controllers
{
    [Route("Todo")]
    [Route("Todos")]
    public class TodoController : Controller
    {
        private readonly ITodoService _todoService;

        public TodoController(ITodoService todoService)
        {
            _todoService = todoService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var todos = await _todoService.GetAsync();
                if (!todos.Any())
                    ViewBag.Info = "No todos found or unable to connect to MongoDB. Please ensure MongoDB is running at localhost:27017.";
                return View(todos);
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                ViewBag.Error = "Failed to connect to MongoDB. Please start MongoDB and retry.";
                Console.Error.WriteLine($"MongoDB connection exception in controller Index: {ex.Message}");
                return View(new List<TodoItem>());
            }
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TodoItem todoItem)
        {
            if (!ModelState.IsValid)
                return View(todoItem);

            await _todoService.CreateAsync(todoItem);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var todo = await _todoService.GetAsync(id);

            if (todo == null)
                return NotFound();

            return View(todo);
        }

        [HttpPost("Edit/{id}")]
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

        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var todo = await _todoService.GetAsync(id);

            if (todo == null)
                return NotFound();

            return View(todo);
        }

        [HttpPost("Delete/{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var todo = await _todoService.GetAsync(id);

            if (todo == null)
                return NotFound();

            await _todoService.RemoveAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("ToggleComplete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleComplete(string id)
        {
            await _todoService.ToggleCompleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Item")]
        public IActionResult Item()
        {
            return View();
        }

        [HttpPost("Item")]
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