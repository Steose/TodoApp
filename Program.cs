using TodoApp.Options; // Option classes
using TodoApp.Repositories; // Repository interfaces and implementations
using TodoApp.Services; // Service interfaces and implementations

var builder = WebApplication.CreateBuilder(args);

// Add MVC services
builder.Services.AddControllersWithViews();

// Bind configuration sections to options classes
builder.Services.Configure<DatabaseProviderOptions>(
    builder.Configuration.GetSection("DatabaseProvider"));

builder.Services.Configure<MongoDbOptions>(
    builder.Configuration.GetSection("MongoDb"));

builder.Services.Configure<CosmosMongoOptions>(
    builder.Configuration.GetSection("CosmosMongo"));

// Register repository and service with dependency injection
builder.Services.AddSingleton<ITodoRepository, TodoRepository>();
builder.Services.AddSingleton<ITodoService, TodoService>();

var app = builder.Build();

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection(); // Redirect HTTP to HTTPS only when a local HTTPS endpoint exists
}
app.UseStaticFiles(); // Serve CSS/JS/images

app.UseRouting();

app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
