using Microsoft.AspNetCore.Mvc; // MVC features
using MongoDB.Driver; // Mongo exceptions
using TodoApp.Models; // TodoItem
using TodoApp.Services; // ITodoService

namespace TodoApp.Controllers
{
    [Route("Todo")] // Base route /Todo
    [Route("Todos")] // Base route /Todos
    public class TodoController : Controller
    {
        private readonly ITodoService _todoService; // Service dependency

        public TodoController(ITodoService todoService)
        {
            _todoService = todoService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        [HttpGet("All")]
        [HttpGet("Items")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get all todo items
                var todos = await _todoService.GetAsync();

                if (!todos.Any())
                {
                    ViewBag.Info = "No todo items found.";
                }

                return View(todos);
            }
            catch (MongoConnectionException ex)
            {
                ViewBag.Error = "Failed to connect to the database.";
                Console.Error.WriteLine($"MongoDB connection error: {ex.Message}");
                return View(new List<TodoItem>());
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An unexpected error occurred while loading todo items.";
                Console.Error.WriteLine($"Unexpected error: {ex.Message}");
                return View(new List<TodoItem>());
            }
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            // Return empty create form
            return View(new TodoItem());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TodoItem todoItem)
        {
            if (!ModelState.IsValid)
            {
                return View(todoItem);
            }

            try
            {
                await _todoService.CreateAsync(todoItem);
            }
            catch (MongoException ex)
            {
                ModelState.AddModelError(string.Empty, "Failed to save the todo item because the database is unavailable.");
                Console.Error.WriteLine($"MongoDB write error: {ex.Message}");
                return View(todoItem);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var todo = await _todoService.GetAsync(id);

            if (todo == null)
            {
                return NotFound();
            }

            return View(todo);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, TodoItem todoItem)
        {
            if (id != todoItem.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(todoItem);
            }

            var existingTodo = await _todoService.GetAsync(id);

            if (existingTodo == null)
            {
                return NotFound();
            }

            try
            {
                await _todoService.UpdateAsync(id, todoItem);
            }
            catch (MongoException ex)
            {
                ModelState.AddModelError(string.Empty, "Failed to update the todo item because the database is unavailable.");
                Console.Error.WriteLine($"MongoDB write error: {ex.Message}");
                return View(todoItem);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var todo = await _todoService.GetAsync(id);

            if (todo == null)
            {
                return NotFound();
            }

            return View(todo);
        }

        [HttpPost("Delete/{id}")]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var todo = await _todoService.GetAsync(id);

            if (todo == null)
            {
                return NotFound();
            }

            try
            {
                await _todoService.RemoveAsync(id);
            }
            catch (MongoException ex)
            {
                ViewBag.Error = "Failed to delete the todo item because the database is unavailable.";
                Console.Error.WriteLine($"MongoDB write error: {ex.Message}");
                return View(todo);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("ToggleComplete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleComplete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            try
            {
                await _todoService.ToggleCompleteAsync(id);
            }
            catch (MongoException ex)
            {
                TempData["Error"] = "Failed to update the todo item because the database is unavailable.";
                Console.Error.WriteLine($"MongoDB write error: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var todo = await _todoService.GetAsync(id);

            if (todo == null)
            {
                return NotFound();
            }

            return View(todo);
        }

        [HttpGet("Item")]
        public IActionResult Item()
        {
            // This route is only for quick-add form
            return View(new TodoItem());
        }

        [HttpPost("Item")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Item(TodoItem todoItem)
        {
            if (!ModelState.IsValid)
            {
                return View(todoItem);
            }

            await _todoService.CreateAsync(todoItem);

            TempData["SuccessMessage"] = "Todo item added successfully!";

            return RedirectToAction(nameof(Index));
        }
    }
}
