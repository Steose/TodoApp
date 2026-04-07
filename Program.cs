using Azure.Identity;
using Microsoft.Extensions.Options;
using TodoApp.Configurations;
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

// Check if Azure Key Vault should be used
bool useAzureKeyVault = builder.Configuration.GetValue<bool>("FeatureFlags:UseAzureKeyVault");

if (useAzureKeyVault)
{
    // Configure Azure Key Vault options
    builder.Services.Configure<AzureKeyVaultOptions>(
        builder.Configuration.GetSection(AzureKeyVaultOptions.SectionName));

    // Get Key Vault URI from configuration
    var keyVaultOptions = builder.Configuration
        .GetSection(AzureKeyVaultOptions.SectionName)
        .Get<AzureKeyVaultOptions>();
    var keyVaultUri = keyVaultOptions?.KeyVaultUri;

    // Register Azure Key Vault as configuration provider
    if (string.IsNullOrEmpty(keyVaultUri))
    {
        throw new InvalidOperationException("Key Vault URI is not configured.");
    }

    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());

    Console.WriteLine("Using Azure Key Vault for configuration");
}
else
{
    Console.WriteLine("Using appsettings.json for configuration");
}

var configuredProvider = builder.Configuration["DatabaseProvider:Provider"]?.Trim();
var useCosmosMongo = builder.Configuration.GetValue<bool>("FeatureFlags:UseCosmosMongo");
var useMongoDb = builder.Configuration.GetValue<bool>("FeatureFlags:UseMongoDb");

if (string.IsNullOrWhiteSpace(configuredProvider))
{
    configuredProvider = useCosmosMongo
        ? "CosmosMongo"
        : useMongoDb
            ? "MongoDb"
            : "InMemory";
    builder.Configuration["DatabaseProvider:Provider"] = configuredProvider;
}

if (string.Equals(configuredProvider, "CosmosMongo", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("Using Cosmos MongoDB repository");
}
else if (string.Equals(configuredProvider, "MongoDb", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("Using MongoDB repository");
}
else
{
    builder.Configuration["DatabaseProvider:Provider"] = "InMemory";
    Console.WriteLine("Using in-memory repository");
}

// Register repository and service with dependency injection
builder.Services.AddSingleton<ITodoRepository>(sp =>
{
    var providerOptions = sp.GetRequiredService<IOptions<DatabaseProviderOptions>>();
    var provider = providerOptions.Value.Provider?.Trim();

    if (string.Equals(provider, "CosmosMongo", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "MongoDb", StringComparison.OrdinalIgnoreCase))
    {
        return ActivatorUtilities.CreateInstance<TodoRepository>(sp);
    }

    return ActivatorUtilities.CreateInstance<InMemoryTodoRepository>(sp);
});

builder.Services.AddSingleton<ITodoService, TodoService>();

var app = builder.Build();

var aspNetCoreUrls = app.Configuration["ASPNETCORE_URLS"];
var hasHttpsEndpoint =
    !string.IsNullOrWhiteSpace(app.Configuration["HTTPS_PORT"]) ||
    (!string.IsNullOrWhiteSpace(aspNetCoreUrls) &&
     aspNetCoreUrls
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)));

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment() && hasHttpsEndpoint)
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
