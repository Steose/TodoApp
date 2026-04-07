using Azure.Identity;
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

builder.Services.Configure<TodoApp.Configurations.MongoDbOptions>(
    builder.Configuration.GetSection("MongoDb"));

builder.Services.Configure<CosmosMongoOptions>(
    builder.Configuration.GetSection("CosmosMongo"));

// Register repository and service with dependency injection
builder.Services.AddSingleton<ITodoRepository, TodoRepository>();
builder.Services.AddSingleton<ITodoService, TodoService>();

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
